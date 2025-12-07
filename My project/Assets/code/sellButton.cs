using MySql.Data.MySqlClient;
using UnityEngine;

public class SellButton : MonoBehaviour
{
    public InventortManager inventortManager;
    public void Sell()
    {
        using (var conn = DataBaseManager.Instance.GetConnection())
        {
            conn.Open();  // 打开连接

            // 查询语句：获取user_inventory和items表中的数据
            string query = @"
                SELECT 
                    ui.user_id,
                    ui.item_id,
                    ui.inventory_id,
                    ui.level,
                    i.rarity,
                    i.base_price
                FROM user_inventory ui
                JOIN items i ON ui.item_id = i.item_id
                WHERE ui.inventory_id = @inventoryId";

            int userId = 0;
            int itemId = 0;
            int level = 0;
            int inventoryId = 0;
            string rarity = "";
            decimal basePrice = 0;
          
            using (var cmd = new MySqlCommand(query, conn))
            {
                // 添加参数
                cmd.Parameters.AddWithValue("@inventoryId", InventortManager.currentInventoryId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        // 获取查询结果
                        userId = reader.GetInt32("user_id");
                        itemId = reader.GetInt32("item_id");
                        inventoryId = reader.GetInt32("inventory_id");
                        level = reader.GetInt32("level");
                        rarity = reader.GetString("rarity");
                        basePrice = reader.GetDecimal("base_price");

                        Debug.Log($"查询结果: 用户ID={userId}, 物品ID={itemId}, 等级={level}, 稀有度={rarity}, 价格={basePrice}");
                    }
                }
            }

            // 插入到market_listings表
            string insertSql = @"
                INSERT INTO market_listings (
                    item_id, 
                    item_name,
                    seller_id, 
                    level, 
                    rarity, 
                    base_price,
                    quantity,
                    created_at
                ) VALUES (
                  @itemId, 
                    (SELECT item_name FROM items WHERE item_id = @itemId),
                    @sellerId, 
                    @level, 
                    @rarity, 
                    @basePrice,
                    @quantity,
                    NOW()
                )";

            using (var insertCmd = new MySqlCommand(insertSql, conn))
            {
                insertCmd.Parameters.AddWithValue("@itemId", itemId);
                insertCmd.Parameters.AddWithValue("@sellerId", userId);
                insertCmd.Parameters.AddWithValue("@level", level);
                insertCmd.Parameters.AddWithValue("@rarity", rarity);
                insertCmd.Parameters.AddWithValue("@basePrice", basePrice);
                insertCmd.Parameters.AddWithValue("@quantity", 1);  // 默认上架1个

                int rows = insertCmd.ExecuteNonQuery();
                Debug.Log($"插入market_listings结果: 影响行数={rows}");
            }
            inventortManager.RemoveItemFromPlayer(inventoryId);
        }
       

        Debug.Log("ddd");
    }
}