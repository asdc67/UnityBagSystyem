using UnityEngine;

public class ClickParticleSpawner : MonoBehaviour
{
    // 在Inspector中拖入你的粒子特效预制体
    public GameObject particleEffectPrefab;
    // 用于坐标转换的相机（主相机或UI相机）
    public Camera targetCamera;

    void Update()
    {
        // 检测鼠标左键点击[citation:4]
        if (Input.GetMouseButtonDown(0))
        {
            SpawnParticleAtMousePosition();
        }
    }

    void SpawnParticleAtMousePosition()
    {
        if (particleEffectPrefab == null || targetCamera == null)
        {
            Debug.LogWarning("请先赋值粒子预制体和目标相机！");
            return;
        }

        // 获取鼠标在屏幕上的位置
        Vector3 mouseScreenPos = Input.mousePosition;
        // 对于UI或2D效果，通常需要固定的Z值（例如到相机的距离）
        mouseScreenPos.z = 10f; // 调整这个值，确保粒子显示在相机前方

        // 将屏幕坐标转换为世界坐标
        Vector3 mouseWorldPos = targetCamera.ScreenToWorldPoint(mouseScreenPos);

        // 实例化粒子特效预制体
        GameObject particleInstance = Instantiate(particleEffectPrefab, mouseWorldPos, Quaternion.identity);

        // 获取ParticleSystem组件并播放
        ParticleSystem ps = particleInstance.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            // 确保从干净状态开始播放[citation:4]
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
            // 可选：在粒子播放完毕后自动销毁实例
            Destroy(particleInstance, ps.main.duration + ps.main.startLifetime.constantMax);
        }
        else
        {
            Debug.LogWarning("实例化的预制体上没有找到ParticleSystem组件！");
        }
    }
}