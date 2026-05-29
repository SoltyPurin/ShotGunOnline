using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GetAxisDown : MonoBehaviour
{
    private float _prevHorizontal = 0f;
    private float _prevVertical = 0f;

    [SerializeField,Header("入力のしきい値")]
    private float threshold = 0.9f;

    public bool LeftDown { get; private set; }
    public bool RightDown { get; private set; }
    public bool UpDown { get; private set; }
    public bool DownDown { get; private set; }

    void Update()
    {
        float h = Input.GetAxis("Horizontal");  
        float v = Input.GetAxis("Vertical"); 

        // 右入力（押した瞬間）
        RightDown = h > threshold && _prevHorizontal <= threshold;
        // 左入力
        LeftDown = h < -threshold && _prevHorizontal >= -threshold;
        // 上入力
        UpDown = v > threshold && _prevVertical <= threshold;
        // 下入力
        DownDown = v < -threshold && _prevVertical >= -threshold;

        // 前フレームの値更新
        _prevHorizontal = h;
        _prevVertical = v;
    }
}
