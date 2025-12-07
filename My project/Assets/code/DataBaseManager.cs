using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MySqlConnector;
using UnityEditor.Experimental.GraphView;

public class DataBaseManager : MonoBehaviour
{
    public static DataBaseManager Instance;
    private string connectionString = "Server=localhost;Database=game;Uid=root;Pwd=147258369l;Port=3306;";
    // Start is called before the first frame update
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
    }

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public MySqlConnection GetConnection()
    {
        return new MySqlConnection(connectionString);
    }
}
