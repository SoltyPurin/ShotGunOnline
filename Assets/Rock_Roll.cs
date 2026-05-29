using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Rock_Roll : MonoBehaviour
{
    private Vector2 velocity;//velocityが使えないため計算用の変数名
    private Vector2 LastPosition;//最後(直前)の場所
    private GameObject Rock;//岩の情報

    [SerializeField,Tooltip("ここにスプライトレンダラーを入れてください")] private SpriteRenderer Rock_SpriteRenderer;//スプライトレンダラー
    [SerializeField,Tooltip("岩の移動量を決定します。初期状態は1です。")] private float MoveSpeed = 1f;//スプライトレンダラー
    private Material targetMaterial;//マテリアルデータ
    private Vector2 prevPos;     // 前フレームの位置
    private Vector2 calcVelocity; // 自前で計算した速度
    void Start()
    {
        //=========================マテリアル設定=========================
        //個別のマテリアルにするためにマテリアルを生成
        Rock_SpriteRenderer.material = Instantiate(Rock_SpriteRenderer.material);

        //操作するマテリアルを生成したマテリアルに設定
        targetMaterial = Rock_SpriteRenderer.material;
        //===============================================================

        //=========================岩の設定=========================
        //岩の情報を取得
        Rock = this.gameObject;

        //初期位置を記録
        prevPos = transform.position; 
        //=========================================================

        //=========================初期化=========================
        //最後の位置を初期化
        LastPosition = transform.position;

        //マテリアルのベクター情報を初期化
        targetMaterial.SetVector("_Vector2", new Vector2(0, 0f));
        //=========================================================
    }

    void Update()
    {
        //NaN防止
        if (Time.deltaTime <= 0f) return; 

        //移動量を計算
        velocity = ((Vector2)Rock.transform.position - LastPosition) / Time.deltaTime * MoveSpeed;

        //=========================丸め処理================================
        const float step = 0.1f; // 丸め幅（0.5f等に変更可）
        velocity = new Vector2(
            Mathf.Floor(velocity.x / step) * step,
            Mathf.Floor(velocity.y / step) * step
        );
        //=========================================================

        //上限指定。方向は保持して上限だけ
        velocity = Vector2.ClampMagnitude(velocity, 10f);
        //直前の位置を保存
        LastPosition = Rock.transform.position;

        //マテリアルのベクター情報を取得
        Vector2 MaterialVector2 = targetMaterial.GetVector("_Vector2");

        //岩の転がる方向を反転させる
        Vector2 current = new Vector2(-MaterialVector2.x, -MaterialVector2.y);

        //current(これまでの移動量)に今の移動量(移動量とかかった時間)を足して計算している。
        Vector2 newValue = current + velocity * Time.deltaTime;

        //マテリアルのベクター情報を更新
        targetMaterial.SetVector("_Vector2", new Vector2(-newValue.x, -newValue.y));
    }

}
