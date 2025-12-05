using UnityEngine;           // Unity引擎
using UnityEngine.UI;        // Unity UI组件
using TMPro;                // TextMeshPro组件
using System.Collections.Generic;  // 泛型集合

public class GameUIController : MonoBehaviour
{
    // 登录面板相关UI元素
    [Header("登录面板")]
    public GameObject loginPanel;           // 登录面板游戏对象
    public TMP_InputField loginUser;        // 登录用户名输入框
    public TMP_InputField loginPass;        // 登录密码输入框
    public Button loginBtn;                 // 登录按钮
    public Button gotoRegisterBtn;          // 跳转到注册按钮
    public TMP_Text loginMsg;               // 登录消息文本

    // 注册面板相关UI元素
    [Header("注册面板")]
    public GameObject registerPanel;        // 注册面板游戏对象
    public TMP_InputField registerUser;     // 注册用户名输入框
    public TMP_InputField registerPass;     // 注册密码输入框
    public TMP_InputField registerConfirm;  // 确认密码输入框
    public Button registerBtn;              // 注册按钮
    public Button gotoLoginBtn;             // 跳转到登录按钮
    public TMP_Text registerMsg;            // 注册消息文本

    // 游戏主面板相关UI元素
    [Header("游戏面板")]
    public GameObject gamePanel;            // 游戏主面板

    public Button logoutBtn;                // 退出登录按钮
    public Button backpackBtn;              // 打开背包按钮

  

    // Start方法在游戏开始时调用
    private void Start()
    {
        // 绑定按钮点击事件
        loginBtn.onClick.AddListener(OnLogin);              // 登录按钮绑定登录方法
        registerBtn.onClick.AddListener(OnRegister);        // 注册按钮绑定注册方法
        gotoRegisterBtn.onClick.AddListener(() => ShowPanel(false));  // 跳转注册按钮绑定显示面板方法
        gotoLoginBtn.onClick.AddListener(() => ShowPanel(true));      // 跳转登录按钮绑定显示面板方法
        logoutBtn.onClick.AddListener(OnLogout);            // 退出登录按钮绑定退出方法
     
        //closeBackpackBtn.onClick.AddListener(HideBackpack); // 关闭背包按钮绑定隐藏背包方法

        // 订阅游戏管理器的事件
        GameManager.OnLoginSuccess += OnLoginSuccess;    // 登录成功事件
        GameManager.OnLoginFailed += OnLoginFailed;      // 登录失败事件
        GameManager.OnLogout += OnLogoutSuccess;         // 退出登录事件

        // 设置初始UI状态
        ShowPanel(true);           // 显示登录面板
        gamePanel.SetActive(false); // 隐藏游戏面板
   
    }

    // 当对象被销毁时调用
    private void OnDestroy()
    {
        // 取消订阅事件，防止内存泄漏
        GameManager.OnLoginSuccess -= OnLoginSuccess;
        GameManager.OnLoginFailed -= OnLoginFailed;
        GameManager.OnLogout -= OnLogoutSuccess;
    }

    // 显示登录或注册面板的方法
    private void ShowPanel(bool showLogin)
    {
        loginPanel.SetActive(showLogin);        // 设置登录面板显示状态
        registerPanel.SetActive(!showLogin);    // 设置注册面板显示状态（与登录面板相反）
        loginMsg.text = "";                     // 清空登录消息
        registerMsg.text = "";                  // 清空注册消息
    }

    // 登录按钮点击处理方法
    private void OnLogin()
    {
        // 获取输入的用户名和密码
        string user = loginUser.text;
        string pass = loginPass.text;

        // 验证输入是否为空
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            ShowMessage(loginMsg, "请输入用户名和密码");  // 显示错误消息
            return;  // 退出方法
        }

        // 禁用登录按钮，防止重复点击
        loginBtn.interactable = false;
        loginMsg.text = "登录中...";  // 显示登录中消息

        // 调用游戏管理器的登录方法
        bool success = GameManager.Instance.Login(user, pass, out string error);
        if (!success)
        {
            loginBtn.interactable = true;  // 登录失败时重新启用按钮
        }
    }

    // 注册按钮点击处理方法
    private void OnRegister()
    {
        // 获取输入的注册信息
        string user = registerUser.text;
        string pass = registerPass.text;
        string confirm = registerConfirm.text;

        // 验证用户名长度
        if (user.Length < 3)
        {
            ShowMessage(registerMsg, "用户名至少3个字符");
            return;
        }

        // 验证密码长度
        if (pass.Length < 6)
        {
            ShowMessage(registerMsg, "密码至少6个字符");
            return;
        }

        // 验证密码确认是否一致
        if (pass != confirm)
        {
            ShowMessage(registerMsg, "密码不一致");
            return;
        }

        // 禁用注册按钮，防止重复点击
        registerBtn.interactable = false;
        registerMsg.text = "注册中...";  // 显示注册中消息

        // 调用游戏管理器的注册方法
        bool success = GameManager.Instance.Register(user, pass, out string error);
        if (success)
        {
            ShowMessage(registerMsg, "注册成功！", true);  // 显示成功消息
            Invoke("SwitchToLogin", 1f);  // 1秒后切换到登录面板
        }
        else
        {
            ShowMessage(registerMsg, error);  // 显示错误消息
            registerBtn.interactable = true;  // 重新启用按钮
        }
    }

    // 切换到登录面板的方法
    private void SwitchToLogin()
    {
        ShowPanel(true);  // 显示登录面板
        registerBtn.interactable = true;  // 重新启用注册按钮
    }

    // 登录成功事件处理方法
    private void OnLoginSuccess()
    {
        ShowMessage(loginMsg, "登录成功！", true);  // 显示成功消息
        loginBtn.interactable = true;  // 重新启用登录按钮

        Invoke("ShowGame", 1f);  // 1秒后显示游戏面板
    }

    // 登录失败事件处理方法
    private void OnLoginFailed(string error)
    {
        ShowMessage(loginMsg, error);  // 显示错误消息
        loginBtn.interactable = true;  // 重新启用登录按钮
    }

    // 显示游戏面板的方法
    private void ShowGame()
    {
        loginPanel.SetActive(false);    // 隐藏登录面板
        registerPanel.SetActive(false); // 隐藏注册面板
        gamePanel.SetActive(true);      // 显示游戏面板

        // 获取当前用户数据并更新UI
        var user = GameManager.CurrentUser;
       
    }

    // 显示背包的方法
   
    // 隐藏背包的方法
  
    // 退出登录按钮点击处理方法
    private void OnLogout()
    {
        GameManager.Instance.Logout();  // 调用游戏管理器的退出登录方法
    }

    // 退出登录成功事件处理方法
    private void OnLogoutSuccess()
    {
        gamePanel.SetActive(false);     // 隐藏游戏面板
    
        loginPanel.SetActive(true);     // 显示登录面板
        // 清空输入框
        loginUser.text = "";
        loginPass.text = "";
        registerUser.text = "";
        registerPass.text = "";
        registerConfirm.text = "";
    }

    // 显示消息的辅助方法
    private void ShowMessage(TMP_Text textObj, string message, bool isSuccess = false)
    {
        textObj.text = message;  // 设置消息文本
        textObj.color = isSuccess ? Color.green : Color.red;  // 根据成功与否设置颜色
    }
}
