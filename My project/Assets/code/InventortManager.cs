using MySql.Data.MySqlClient;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventortManager : MonoBehaviour
{
    // 单例实例
    public static InventortManager Instance;

    // Awake方法
    private void Awake()
    {
        // 单例模式初始化
        if (Instance == null)
        {
            Instance = this;
        }
    }

    // 获取玩家背包物品列表的方法
    public List<InventoryItem> GetPlayerInventory(int userId)
    {
        // 创建空的背包物品列表
        var inventory = new List<InventoryItem>();

        // 使用数据库连接
        using (var conn = DataBaseManager.Instance.GetConnection())
        {
            conn.Open();  // 打开连接

            // SQL查询语句：联表查询背包物品和物品模板信息
            string sql = @"
                SELECT ui.inventory_id, ui.item_id, ui.quantity, ui.slot_index,
                       i.item_name, i.item_type
                FROM user_inventory ui
                JOIN items i ON ui.item_id = i.item_id
                WHERE ui.user_id = @userId
                ORDER BY ui.slot_index";  // 按槽位索引排序

            using (var cmd = new MySqlCommand(sql, conn))
            {
                // 添加用户ID参数
                cmd.Parameters.AddWithValue("@userId", userId);

                // 执行查询并读取结果
                using (var reader = cmd.ExecuteReader())
                {
                    // 循环读取每一行数据
                    while (reader.Read())
                    {
                        // 创建背包物品对象并填充数据
                        var item = new InventoryItem
                        {
                            inventoryId = reader.GetInt32("inventory_id"),  // 背包记录ID
                            itemId = reader.GetInt32("item_id"),            // 物品ID
                            itemName = reader.GetString("item_name"),       // 物品名称
                            itemType = reader.GetString("item_type"),       // 物品类型
                            quantity = reader.GetInt32("quantity"),         // 物品数量
                            slotIndex = reader.GetInt32("slot_index")       // 槽位索引
                        };
                        // 将物品添加到列表中
                        inventory.Add(item);
                    }
                }
            }
        }

        // 返回背包物品列表
        return inventory;
    }

    // 添加物品到玩家背包的方法
    public bool AddItemToPlayer(int userId, int itemId, int quantity = 1)
    {
        // 使用数据库连接
        using (var conn = DataBaseManager.Instance.GetConnection())
        {
            conn.Open();  // 打开连接

            // 检查玩家是否已拥有该物品的SQL语句
            string checkSql = "SELECT inventory_id, quantity FROM user_inventory WHERE user_id = @userId AND item_id = @itemId";
            using (var checkCmd = new MySqlCommand(checkSql, conn))
            {
                // 添加参数
                checkCmd.Parameters.AddWithValue("@userId", userId);
                checkCmd.Parameters.AddWithValue("@itemId", itemId);

                // 执行查询
                using (var reader = checkCmd.ExecuteReader())
                {
                    // 如果玩家已拥有该物品
                    if (reader.Read())
                    {
                        // 关闭读取器，以便执行更新操作
                        reader.Close();
                        // 更新物品数量的SQL语句
                        string updateSql = "UPDATE user_inventory SET quantity = quantity + @quantity WHERE user_id = @userId AND item_id = @itemId";
                        using (var updateCmd = new MySqlCommand(updateSql, conn))
                        {
                            // 添加参数
                            updateCmd.Parameters.AddWithValue("@userId", userId);
                            updateCmd.Parameters.AddWithValue("@itemId", itemId);
                            updateCmd.Parameters.AddWithValue("@quantity", quantity);
                            // 执行更新操作
                            updateCmd.ExecuteNonQuery();
                        }
                        return true;  // 添加成功
                    }
                }
            }

            // 如果玩家没有该物品，插入新记录
            string insertSql = "INSERT INTO user_inventory (user_id, item_id, quantity) VALUES (@userId, @itemId, @quantity)";
            using (var insertCmd = new MySqlCommand(insertSql, conn))
            {
                // 添加参数
                insertCmd.Parameters.AddWithValue("@userId", userId);
                insertCmd.Parameters.AddWithValue("@itemId", itemId);
                insertCmd.Parameters.AddWithValue("@quantity", quantity);
                // 执行插入操作
                insertCmd.ExecuteNonQuery();
            }

            return true;  // 添加成功
        }
    }
}
