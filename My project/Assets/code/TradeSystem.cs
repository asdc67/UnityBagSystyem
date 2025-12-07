using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MySqlConnector;

public class TradeSystemManager : MonoBehaviour
{
    public static TradeSystemManager Instance;

    [Header("UI References")]
    public GameObject tradePanel;
    public Transform myItemsGrid;          // 我提供的物品
    public Transform otherItemsGrid;       // 对方提供的物品
    public TextMeshProUGUI myGoldText;     // 我提供的金币
    public TextMeshProUGUI otherGoldText;  // 对方提供的金币
    public TextMeshProUGUI statusText;     // 交易状态
    public TextMeshProUGUI otherPlayerNameText;  // 对方玩家名字
    public Button confirmButton;           // 确认按钮
    public Button cancelButton;            // 取消按钮
    public Button addGoldButton;           // 添加金币按钮
    public TMP_InputField goldInputField;  // 金币输入框

    [Header("Prefabs")]
    public GameObject tradeItemSlotPrefab;  // 交易物品槽位

    // 当前交易数据
    private int currentTradeId = -1;
    private int otherPlayerId = -1;
    private string otherPlayerName = "";
    private bool isInitiator = false;  // 是否是发起者

    // 交易状态
    private bool isPlayerConfirmed = false;
    private bool isOtherConfirmed = false;

    // 交易内容
    private Dictionary<int, TradeItemData> playerOfferedItems = new Dictionary<int, TradeItemData>();
    private Dictionary<int, TradeItemData> otherOfferedItems = new Dictionary<int, TradeItemData>();
    private int playerOfferedGold = 0;
    private int otherOfferedGold = 0;

    // 交易物品数据类
    [System.Serializable]
    public class TradeItemData
    {
        public int itemId;
        public int quantity;
        public int level;
        public bool isWeapon;
        public int inventoryId;  // 背包中的物品ID
    }
    // 添加在文件的合适位置（如类内部）
    [System.Serializable]
    public class TradeItemSlot
    {
        public int itemId;
        public int quantity;
        public int level;
        public bool isWeapon;
        public int inventoryId;

        public TradeItemSlot(int id, int qty, int lvl, bool weapon, int invId = -1)
        {
            itemId = id;
            quantity = qty;
            level = lvl;
            isWeapon = weapon;
            inventoryId = invId;
        }
        public void Setup(int id, int qty, int lvl, bool weapon, bool isMyItem)
        {
            itemId = id;
            quantity = qty;
            level = lvl;
            isWeapon = weapon;
        }


    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // 初始化UI事件
        confirmButton.onClick.AddListener(ConfirmTrade);
        cancelButton.onClick.AddListener(CancelTrade);
        addGoldButton.onClick.AddListener(AddGoldToTrade);
    }

    // 发起交易
    public void InitiateTrade(int targetPlayerId, string targetPlayerName)
    {
        otherPlayerId = targetPlayerId;
        otherPlayerName = targetPlayerName;
        isInitiator = true;

        StartCoroutine(CreateTradeCoroutine());
    }

    // 接收交易请求
    public void ReceiveTradeRequest(int tradeId, int fromPlayerId, string fromPlayerName)
    {
        otherPlayerId = fromPlayerId;
        otherPlayerName = fromPlayerName;
        currentTradeId = tradeId;
        isInitiator = false;

        // 显示交易面板
        ShowTradePanel();

        // 加载交易数据
        StartCoroutine(LoadTradeData());
    }

    // 显示交易面板
    private void ShowTradePanel()
    {
        tradePanel.SetActive(true);
        otherPlayerNameText.text = otherPlayerName;
        UpdateStatus("等待对方添加物品...");

        // 重置UI
        ClearTradeUI();
    }

    // 隐藏交易面板
    public void HideTradePanel()
    {
        tradePanel.SetActive(false);
        ResetTradeData();
    }

    // 重置交易数据
    private void ResetTradeData()
    {
        currentTradeId = -1;
        otherPlayerId = -1;
        otherPlayerName = "";
        isPlayerConfirmed = false;
        isOtherConfirmed = false;
        playerOfferedItems.Clear();
        otherOfferedItems.Clear();
        playerOfferedGold = 0;
        otherOfferedGold = 0;
    }

    // 从背包中添加物品到交易
    public void AddItemFromBackpack(int itemId, int quantity, int level, bool isWeapon, int inventoryId = -1)
    {
        if (currentTradeId == -1)
        {
            Debug.LogWarning("没有进行中的交易");
            return;
        }

        TradeItemData itemData = new TradeItemData
        {
            itemId = itemId,
            quantity = quantity,
            level = level,
            isWeapon = isWeapon,
            inventoryId = inventoryId
        };

        StartCoroutine(AddItemToTradeCoroutine(itemData));
    }

    // 创建交易协程
    private IEnumerator CreateTradeCoroutine()
    {
        string query = @"INSERT INTO player_trades (player1_id, player2_id, status) 
                        VALUES (@player1, @player2, 'pending')";

        using (MySqlConnection conn = DataBaseManager.Instance.GetConnection())
        {
            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@player1", GetCurrentPlayerId());
                cmd.Parameters.AddWithValue("@player2", otherPlayerId);

                cmd.ExecuteNonQuery();
                currentTradeId = (int)cmd.LastInsertedId;

                Debug.Log($"交易创建成功，ID: {currentTradeId}");

                // 显示交易面板
                ShowTradePanel();

                // TODO: 发送网络消息给目标玩家
                // NetworkManager.Instance.SendTradeRequest(otherPlayerId, currentTradeId, GetCurrentPlayerName());
            }
            catch (Exception e)
            {
                Debug.LogError($"创建交易失败: {e.Message}");
            }
        }

        yield return null;
    }

    // 添加物品到交易
    private IEnumerator AddItemToTradeCoroutine(TradeItemData itemData)
    {
        // 检查物品是否可交易
        if (!CheckItemTradable(itemData.itemId, itemData.quantity))
        {
            Debug.LogWarning("物品不可交易或数量不足");
            yield break;
        }

        // 添加到本地数据
        playerOfferedItems[itemData.itemId] = itemData;

        // 保存到数据库
        string query = @"INSERT INTO trade_items 
                        (trade_id, from_user_id, to_user_id, item_id, inventory_id, quantity, level, is_weapon) 
                        VALUES (@tradeId, @fromUser, @toUser, @itemId, @inventoryId, @quantity, @level, @isWeapon)";

        using (MySqlConnection conn = DataBaseManager.Instance.GetConnection())
        {
            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tradeId", currentTradeId);
                cmd.Parameters.AddWithValue("@fromUser", GetCurrentPlayerId());
                cmd.Parameters.AddWithValue("@toUser", otherPlayerId);
                cmd.Parameters.AddWithValue("@itemId", itemData.itemId);
                cmd.Parameters.AddWithValue("@inventoryId", itemData.inventoryId);
                cmd.Parameters.AddWithValue("@quantity", itemData.quantity);
                cmd.Parameters.AddWithValue("@level", itemData.level);
                cmd.Parameters.AddWithValue("@isWeapon", itemData.isWeapon);

                cmd.ExecuteNonQuery();

                // 更新UI
                UpdateTradeUI();

                // TODO: 发送网络消息给对手
                // NetworkManager.Instance.SendTradeItemAdded(otherPlayerId, itemData);
            }
            catch (Exception e)
            {
                Debug.LogError($"添加物品失败: {e.Message}");
            }
        }

        yield return null;
    }

    // 添加金币到交易
    private void AddGoldToTrade()
    {
        if (!int.TryParse(goldInputField.text, out int goldAmount) || goldAmount <= 0)
        {
            Debug.LogWarning("请输入有效的金币数量");
            return;
        }

        StartCoroutine(AddGoldToTradeCoroutine(goldAmount));
    }

    private IEnumerator AddGoldToTradeCoroutine(int goldAmount)
    {
        // 检查金币是否足够
        int currentGold = GetPlayerGold();
        if (currentGold < goldAmount)
        {
            Debug.LogWarning("金币不足");
            yield break;
        }

        playerOfferedGold += goldAmount;

        // 保存到数据库
        string query = @"INSERT INTO trade_gold (trade_id, from_user_id, to_user_id, gold_amount) 
                        VALUES (@tradeId, @fromUser, @toUser, @goldAmount)";

        using (MySqlConnection conn = DataBaseManager.Instance.GetConnection())
        {
            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tradeId", currentTradeId);
                cmd.Parameters.AddWithValue("@fromUser", GetCurrentPlayerId());
                cmd.Parameters.AddWithValue("@toUser", otherPlayerId);
                cmd.Parameters.AddWithValue("@goldAmount", goldAmount);

                cmd.ExecuteNonQuery();

                // 更新UI
                UpdateTradeUI();

                // TODO: 发送网络消息
                // NetworkManager.Instance.SendTradeGoldAdded(otherPlayerId, goldAmount);

                // 清空输入框
                goldInputField.text = "";
            }
            catch (Exception e)
            {
                Debug.LogError($"添加金币失败: {e.Message}");
            }
        }

        yield return null;
    }

    // 确认交易
    private void ConfirmTrade()
    {
        StartCoroutine(ConfirmTradeCoroutine());
    }

    private IEnumerator ConfirmTradeCoroutine()
    {
        isPlayerConfirmed = true;

        // 更新数据库
        string updateQuery = "";
        if (GetCurrentPlayerId() == otherPlayerId)  // 防止错误
        {
            Debug.LogError("玩家ID相同，无法交易");
            yield break;
        }

        if (isInitiator)
        {
            updateQuery = "UPDATE player_trades SET player1_confirmed = TRUE, status = 'confirmed' WHERE trade_id = @tradeId";
        }
        else
        {
            updateQuery = "UPDATE player_trades SET player2_confirmed = TRUE, status = 'confirmed' WHERE trade_id = @tradeId";
        }

        using (MySqlConnection conn = DataBaseManager.Instance.GetConnection())
        {
            bool success = false;
            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(updateQuery, conn);
                cmd.Parameters.AddWithValue("@tradeId", currentTradeId);

                int rows = cmd.ExecuteNonQuery();

                if (rows > 0)
                {
                    UpdateStatus("已确认，等待对方确认...");
                    confirmButton.interactable = false;

                    // TODO: 发送网络消息
                    // NetworkManager.Instance.SendTradeConfirmed(otherPlayerId);

                    // 检查是否双方都已确认
                    success = false;
                    
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"确认交易失败: {e.Message}");
                success = false;
            }
            if(success)
            {
                yield return StartCoroutine(CheckTradeConfirmation());
            }
        }
    }

    // 检查交易确认状态
    private IEnumerator CheckTradeConfirmation()
    {
        string query = "SELECT player1_confirmed, player2_confirmed FROM player_trades WHERE trade_id = @tradeId";
        bool shouldExecuteTrade = false;
        Exception error = null;

        // 第一阶段：数据库查询（包含在try-catch中）
        using (MySqlConnection conn = DataBaseManager.Instance.GetConnection())
        {
            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tradeId", currentTradeId);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        bool p1Confirmed = reader.GetBoolean(0);
                        bool p2Confirmed = reader.GetBoolean(1);

                        if (p1Confirmed && p2Confirmed)
                        {
                            shouldExecuteTrade = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
                Debug.LogError($"检查确认状态失败: {e.Message}");
            }
        }

        // 第二阶段：执行交易（在try-catch外部使用yield）
        if (shouldExecuteTrade)
        {
            yield return StartCoroutine(ExecuteTrade());
        }
        else if (error != null)
        {
            // 可以在这里处理错误情况
            Debug.Log("交易确认检查失败，无法继续");
        }
    }

    // 执行交易
    private IEnumerator ExecuteTrade()
    {
        UpdateStatus("正在完成交易...");

        using (MySqlConnection conn = DataBaseManager.Instance.GetConnection())
        {
            try
            {
                conn.Open();

                // 使用事务确保数据一致性
                MySqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // 1. 获取交易双方ID
                    string getPlayersQuery = "SELECT player1_id, player2_id FROM player_trades WHERE trade_id = @tradeId";
                    MySqlCommand cmd1 = new MySqlCommand(getPlayersQuery, conn, transaction);
                    cmd1.Parameters.AddWithValue("@tradeId", currentTradeId);

                    int player1Id = 0, player2Id = 0;
                    using (var reader = cmd1.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            player1Id = reader.GetInt32(0);
                            player2Id = reader.GetInt32(1);
                        }
                    }

                    // 2. 转移金币
                    string transferGoldQuery = @"
                        -- 从提供方扣除金币
                        UPDATE users 
                        SET gold = gold - (
                            SELECT COALESCE(SUM(gold_amount), 0) 
                            FROM trade_gold 
                            WHERE trade_id = @tradeId AND from_user_id = user_id
                        )
                        WHERE user_id IN (SELECT from_user_id FROM trade_gold WHERE trade_id = @tradeId);
                        
                        -- 添加到接收方
                        UPDATE users 
                        SET gold = gold + (
                            SELECT COALESCE(SUM(gold_amount), 0) 
                            FROM trade_gold 
                            WHERE trade_id = @tradeId AND to_user_id = user_id
                        )
                        WHERE user_id IN (SELECT to_user_id FROM trade_gold WHERE trade_id = @tradeId);
                    ";

                    MySqlCommand cmd2 = new MySqlCommand(transferGoldQuery, conn, transaction);
                    cmd2.Parameters.AddWithValue("@tradeId", currentTradeId);
                    cmd2.ExecuteNonQuery();

                    // 3. 转移物品
                    string transferItemsQuery = @"
                        -- 从提供方移除物品
                        UPDATE user_inventory 
                        SET quantity = quantity - (
                            SELECT quantity 
                            FROM trade_items 
                            WHERE trade_id = @tradeId 
                            AND item_id = user_inventory.item_id
                            AND inventory_id = user_inventory.inventory_id
                        )
                        WHERE inventory_id IN (
                            SELECT inventory_id 
                            FROM trade_items 
                            WHERE trade_id = @tradeId
                        );
                        
                        -- 添加到接收方
                        INSERT INTO user_inventory (user_id, item_id, quantity, level, slot_index)
                        SELECT 
                            t.to_user_id,
                            t.item_id,
                            t.quantity,
                            t.level,
                            (SELECT COALESCE(MAX(slot_index), 0) + 1 
                             FROM user_inventory 
                             WHERE user_id = t.to_user_id) as new_slot
                        FROM trade_items t
                        WHERE t.trade_id = @tradeId
                        ON DUPLICATE KEY UPDATE quantity = quantity + VALUES(quantity);
                    ";

                    MySqlCommand cmd3 = new MySqlCommand(transferItemsQuery, conn, transaction);
                    cmd3.Parameters.AddWithValue("@tradeId", currentTradeId);
                    cmd3.ExecuteNonQuery();

                    // 4. 清理数量为0的物品
                    string cleanupQuery = "DELETE FROM user_inventory WHERE quantity <= 0";
                    MySqlCommand cmd4 = new MySqlCommand(cleanupQuery, conn, transaction);
                    cmd4.ExecuteNonQuery();

                    // 5. 更新交易状态
                    string updateStatusQuery = "UPDATE player_trades SET status = 'completed', completed_at = NOW() WHERE trade_id = @tradeId";
                    MySqlCommand cmd5 = new MySqlCommand(updateStatusQuery, conn, transaction);
                    cmd5.Parameters.AddWithValue("@tradeId", currentTradeId);
                    cmd5.ExecuteNonQuery();

                    // 提交事务
                    transaction.Commit();

                    Debug.Log("交易完成成功！");
                    UpdateStatus("交易完成！");

                    // 刷新背包数据
                    if (InventortManager.Instance != null)
                    {
                        InventortManager.Instance.LoadPlayerItems();
                    }

                    // 3秒后关闭面板
                    Invoke("HideTradePanel", 3f);
                }
                catch (Exception e)
                {
                    transaction.Rollback();
                    Debug.LogError($"交易执行失败: {e.Message}");
                    UpdateStatus("交易失败: " + e.Message);

                    // 更新交易状态为失败
                    string failQuery = "UPDATE player_trades SET status = 'failed' WHERE trade_id = @tradeId";
                    MySqlCommand failCmd = new MySqlCommand(failQuery, conn);
                    failCmd.Parameters.AddWithValue("@tradeId", currentTradeId);
                    failCmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"数据库连接失败: {e.Message}");
            }
        }

        yield return null;
    }

    // 取消交易
    private void CancelTrade()
    {
        StartCoroutine(CancelTradeCoroutine());
    }

    private IEnumerator CancelTradeCoroutine()
    {
        if (currentTradeId == -1)
        {
            HideTradePanel();
            yield break;
        }

        string query = "UPDATE player_trades SET status = 'cancelled', cancelled_at = NOW() WHERE trade_id = @tradeId";

        using (MySqlConnection conn = DataBaseManager.Instance.GetConnection())
        {
            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tradeId", currentTradeId);
                cmd.ExecuteNonQuery();

                Debug.Log("交易已取消");
                HideTradePanel();

                // TODO: 发送网络消息
                // NetworkManager.Instance.SendTradeCancelled(otherPlayerId);
            }
            catch (Exception e)
            {
                Debug.LogError($"取消交易失败: {e.Message}");
            }
        }

        yield return null;
    }

    // 加载交易数据
    private IEnumerator LoadTradeData()
    {
        string query = @"
            SELECT 
                ti.item_id,
                ti.from_user_id,
                ti.quantity,
                ti.level,
                ti.is_weapon,
                tg.gold_amount
            FROM player_trades pt
            LEFT JOIN trade_items ti ON pt.trade_id = ti.trade_id
            LEFT JOIN trade_gold tg ON pt.trade_id = tg.trade_id
            WHERE pt.trade_id = @tradeId
        ";

        using (MySqlConnection conn = DataBaseManager.Instance.GetConnection())
        {
            try
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@tradeId", currentTradeId);

                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))  // 有物品
                        {
                            int itemId = reader.GetInt32(0);
                            int fromUserId = reader.GetInt32(1);
                            int quantity = reader.GetInt32(2);
                            int level = reader.GetInt32(3);
                            bool isWeapon = reader.GetBoolean(4);

                            TradeItemData itemData = new TradeItemData
                            {
                                itemId = itemId,
                                quantity = quantity,
                                level = level,
                                isWeapon = isWeapon
                            };

                            if (fromUserId == GetCurrentPlayerId())
                            {
                                playerOfferedItems[itemId] = itemData;
                            }
                            else
                            {
                                otherOfferedItems[itemId] = itemData;
                            }
                        }

                        if (!reader.IsDBNull(5))  // 有金币
                        {
                            int goldAmount = reader.GetInt32(5);
                            int fromUserId = reader.GetInt32(1);

                            if (fromUserId == GetCurrentPlayerId())
                            {
                                playerOfferedGold += goldAmount;
                            }
                            else
                            {
                                otherOfferedGold += goldAmount;
                            }
                        }
                    }
                }

                // 更新UI
                UpdateTradeUI();
            }
            catch (Exception e)
            {
                Debug.LogError($"加载交易数据失败: {e.Message}");
            }
        }

        yield return null;
    }

    // 更新交易UI
    private void UpdateTradeUI()
    {
        // 清空现有UI
        ClearTradeUI();

        // 更新金币显示
        myGoldText.text = $"我的金币: {playerOfferedGold}";
        otherGoldText.text = $"对方金币: {otherOfferedGold}";

        // 显示我的物品
        foreach (var item in playerOfferedItems.Values)
        {
            GameObject slotObj = Instantiate(tradeItemSlotPrefab, myItemsGrid);
            TradeItemSlot slot = slotObj.GetComponent<TradeItemSlot>();
            if (slot != null)
            {
                slot.Setup(item.itemId, item.quantity, item.level, item.isWeapon, true);
            }
        }

        // 显示对方物品
        foreach (var item in otherOfferedItems.Values)
        {
            GameObject slotObj = Instantiate(tradeItemSlotPrefab, otherItemsGrid);
            TradeItemSlot slot = slotObj.GetComponent<TradeItemSlot>();
            if (slot != null)
            {
                slot.Setup(item.itemId, item.quantity, item.level, item.isWeapon, false);
            }
        }

        // 更新按钮状态
        confirmButton.interactable = (playerOfferedItems.Count > 0 || playerOfferedGold > 0) && !isPlayerConfirmed;
    }

    // 清空UI
    private void ClearTradeUI()
    {
        foreach (Transform child in myItemsGrid)
            Destroy(child.gameObject);
        foreach (Transform child in otherItemsGrid)
            Destroy(child.gameObject);
    }

    // 更新状态文本
    private void UpdateStatus(string message)
    {
        statusText.text = message;
    }

    // 检查物品是否可交易
    private bool CheckItemTradable(int itemId, int quantity)
    {
        // 这里可以添加更多检查逻辑
        // 比如：物品是否绑定、是否在冷却中等等

        return true;  // 暂时默认都可交易
    }

    // 辅助方法
    private int GetCurrentPlayerId()
    {
        // 这里需要根据您的玩家系统返回当前玩家ID
        // 假设存储在PlayerPrefs中
        return PlayerPrefs.GetInt("PlayerID", 0);
    }

    private string GetCurrentPlayerName()
    {
        return PlayerPrefs.GetString("PlayerName", "Player");
    }

    private int GetPlayerGold()
    {
        // 这里需要从您的玩家数据系统中获取金币
        return PlayerPrefs.GetInt("PlayerGold", 0);
    }

    // 网络消息处理（占位符）
    public void HandleTradeRequest(int tradeId, int fromPlayerId, string fromPlayerName)
    {
        ReceiveTradeRequest(tradeId, fromPlayerId, fromPlayerName);
    }

    public void HandleTradeItemAdded(TradeItemData itemData)
    {
        otherOfferedItems[itemData.itemId] = itemData;
        UpdateTradeUI();
    }

    public void HandleTradeGoldAdded(int goldAmount)
    {
        otherOfferedGold += goldAmount;
        UpdateTradeUI();
    }

    public void HandleTradeConfirmed()
    {
        isOtherConfirmed = true;
        UpdateStatus("对方已确认");

        // 检查是否双方都已确认
        if (isPlayerConfirmed && isOtherConfirmed)
        {
            StartCoroutine(ExecuteTrade());
        }
    }
}