using MySql.Data.MySqlClient;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 单例实例
    public static GameManager Instance;

    // 当前登录用户的数据属性（只读）
    public static UserData CurrentUser { get; private set; }

    // 定义事件，用于通知其他组件登录状态变化
    public static System.Action OnLoginSuccess;           // 登录成功事件
    public static System.Action<string> OnLoginFailed;    // 登录失败事件（带错误信息）
    public static System.Action OnLogout;                 // 退出登录事件

    // Awake方法在对象初始化时调用
    private void Awake()
    {
        // 单例模式初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 用户注册方法
    public bool Register(string username, string password, out string error)
    {
        // 初始化错误信息为空字符串
        error = "";

        // 验证用户名长度
        if (username.Length < 3)
        {
            error = "用户名至少3个字符";
            return false;  // 注册失败
        }

        // 验证密码长度
        if (password.Length < 6)
        {
            error = "密码至少6个字符";
            return false;  // 注册失败
        }

        // 使用using语句确保数据库连接在使用后被正确关闭
        using (var conn = DataBaseManager.Instance.GetConnection())
        {
            // 打开数据库连接
            conn.Open();

            // 检查用户名是否已存在的SQL语句
            string checkSql = "SELECT user_id FROM users WHERE username = @username";
            using (var cmd = new MySqlCommand(checkSql, conn))
            {
                // 添加参数，防止SQL注入攻击
                cmd.Parameters.AddWithValue("@username", username);
                // 执行查询，如果返回结果说明用户名已存在
                if (cmd.ExecuteScalar() != null)
                {
                    error = "用户名已存在";
                    return false;  // 注册失败
                }
            }

            // 插入新用户的SQL语句
            string insertSql = "INSERT INTO users (username, password_hash) VALUES (@username, @password)";
            using (var cmd = new MySqlCommand(insertSql, conn))
            {
                // 添加参数
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@password", PasswordHelper.HashPassword(password));
                // 执行插入操作
                cmd.ExecuteNonQuery();
            }
        }

        // 在控制台输出注册成功信息
        Debug.Log($"注册成功: {username}");
        return true;  // 注册成功
    }

    // 用户登录方法
    public bool Login(string username, string password, out string error)
    {
        error = "";

        using (var conn = DataBaseManager.Instance.GetConnection())
        {
            conn.Open();

            string sql = "SELECT user_id, username, password_hash, level, gold FROM users WHERE username = @username";

            using (var cmd = new MySqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        error = "用户不存在";
                        OnLoginFailed?.Invoke(error);
                        return false;
                    }

                    // 读取用户数据
                    int userId = reader.GetInt32("user_id");
                    string storedHash = reader.GetString("password_hash");
                    string dbUsername = reader.GetString("username");
                    int level = reader.GetInt32("level");
                    int gold = reader.GetInt32("gold");

                    // 重要：先关闭第一个DataReader
                    reader.Close();

                    // 现在可以执行新的查询
                    string checkSql = "SELECT user_id FROM users WHERE username = @user AND password_hash = @hash";

                    using (var checkCmd = new MySqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@user", username);
                        checkCmd.Parameters.AddWithValue("@hash", PasswordHelper.HashPassword(password));

                        if (checkCmd.ExecuteScalar() == null)
                        {
                            error = "密码错误";
                            OnLoginFailed?.Invoke(error);
                            return false;
                        }
                    }

                    // 登录成功
                    CurrentUser = new UserData
                    {
                        userId = userId,
                        username = dbUsername,
                        level = level,
                        gold = gold
                    };

                    OnLoginSuccess?.Invoke();
                    return true;
                }
            }
        }
    }
    // 退出登录方法
    public void Logout()
    {
        CurrentUser = null;        // 清空当前用户数据
        OnLogout?.Invoke();        // 触发退出登录事件
    }
}

// 用户数据类，用于存储用户信息
[System.Serializable]  // 使该类可序列化，便于在Inspector中查看
public class UserData
{
    public int userId;        // 用户ID
    public string username;   // 用户名
    public int level;         // 等级
    public int gold;          // 金币数量
}

// 物品数据类，用于存储物品信息
[System.Serializable]
public class ItemData
{
    public int itemId;        // 物品ID
    public string itemName;   // 物品名称
    public string itemType;   // 物品类型
    public string rarity;     // 稀有度
    public string description; // 物品描述
    public string iconName;   // 图标名称
    public int maxStack;      // 最大堆叠数
    public int basePrice;     // 基础价格
}

// 背包物品类，用于存储玩家背包中的物品信息
[System.Serializable]
public class InventoryItem
{
    public int inventoryId;   // 背包记录ID
    public int itemId;        // 物品ID
    public string itemName;   // 物品名称
    public string itemType;   // 物品类型
    public int quantity;      // 物品数量
    public int slotIndex;     // 背包槽位索引
}


