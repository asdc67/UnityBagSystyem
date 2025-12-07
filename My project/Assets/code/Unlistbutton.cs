using MySql.Data.MySqlClient;
using UnityEngine;

public class SimpleUnlistButton : MonoBehaviour
{
    public InventortManager inventortManager;

    public void Unlist(int listingId)
    {
        using (var conn = DataBaseManager.Instance.GetConnection())
        {
            conn.Open();

            // 1. 查询上架信息
            string query = "SELECT seller_id, item_id, quantity, level FROM market_listings WHERE listing_id = @listingId";

            int sellerId = 0;
            int itemId = 0;
            int quantity = 0;
            int level = 0;

            using (var cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@listingId", listingId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        sellerId = reader.GetInt32("seller_id");
                        itemId = reader.GetInt32("item_id");
                        quantity = reader.GetInt32("quantity");
                        level = reader.GetInt32("level");
                    }
                }
            }

            // 2. 检查权限
            int currentUserId = GameManager.CurrentUser?.userId ?? 0;
            if (sellerId != currentUserId)
            {
                Debug.Log("权限不足，无法下架");
                return;
            }

            // 3. 删除上架记录
            string deleteSql = "DELETE FROM market_listings WHERE listing_id = @listingId";
            using (var deleteCmd = new MySqlCommand(deleteSql, conn))
            {
                deleteCmd.Parameters.AddWithValue("@listingId", listingId);
                deleteCmd.ExecuteNonQuery();
            }

            // 4. 添加到背包
            AddItemToPlayer(currentUserId, itemId, quantity, level);

            // 5. 刷新背包
            if (inventortManager != null)
            {
                inventortManager.LoadPlayerItems();
            }
        }
    }

    // 与您图片中的RemoveItemFromPlayer类似，这个是添加
    public void AddItemToPlayer(int userId, int itemId, int quantity, int level)
    {
        using (var conn = DataBaseManager.Instance.GetConnection())
        {
            conn.Open();

            // 检查玩家是否已拥有该物品
            string checkSql = "SELECT inventory_id FROM user_inventory WHERE user_id = @userId AND item_id = @itemId";
            bool hasItem = false;

            using (var checkCmd = new MySqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@userId", userId);
                checkCmd.Parameters.AddWithValue("@itemId", itemId);

                using (var reader = checkCmd.ExecuteReader())
                {
                    hasItem = reader.Read();
                }
            }

            if (hasItem)
            {
                // 更新数量
                string updateSql = "UPDATE user_inventory SET quantity = quantity + @quantity WHERE user_id = @userId AND item_id = @itemId";
                using (var updateCmd = new MySqlCommand(updateSql, conn))
                {
                    updateCmd.Parameters.AddWithValue("@userId", userId);
                    updateCmd.Parameters.AddWithValue("@itemId", itemId);
                    updateCmd.Parameters.AddWithValue("@quantity", quantity);
                    updateCmd.ExecuteNonQuery();
                }
            }
            else
            {
                // 添加新物品
                string insertSql = "INSERT INTO user_inventory (user_id, item_id, quantity, level) VALUES (@userId, @itemId, @quantity, @level)";
                using (var insertCmd = new MySqlCommand(insertSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("@userId", userId);
                    insertCmd.Parameters.AddWithValue("@itemId", itemId);
                    insertCmd.Parameters.AddWithValue("@quantity", quantity);
                    insertCmd.Parameters.AddWithValue("@level", level);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }
    }
}