using System.Security.Cryptography;  // 引入加密命名空间
using System.Text;                   // 引入文本编码命名空间

public static class PasswordHelper
{
    // 对密码进行MD5哈希处理的方法
    public static string HashPassword(string password)
    {
        // 使用using语句确保MD5对象在使用后被正确释放
        using (var md5 = MD5.Create())
        {
            // 将密码字符串转换为UTF8编码的字节数组
            byte[] bytes = Encoding.UTF8.GetBytes(password);
            // 计算字节数组的MD5哈希值
            byte[] hash = md5.ComputeHash(bytes);

            // 创建StringBuilder来构建十六进制字符串
            StringBuilder sb = new StringBuilder();
            // 遍历哈希字节数组中的每个字节
            foreach (byte b in hash)
            {
                // 将每个字节转换为两位十六进制数字并添加到字符串中
                sb.Append(b.ToString("x2"));
            }
            // 返回完整的MD5哈希字符串
            return sb.ToString();
        }
    }

    // 验证密码是否正确的方法
    public static bool VerifyPassword(string password, string hash)
    {
        // 计算输入密码的哈希值，并与存储的哈希值进行比较
        return HashPassword(password) == hash;
    }
}
