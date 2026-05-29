using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BreakProtocol : MonoBehaviour
{
    [SerializeField, Header("タイルマップ登録")]
    private Tilemap _tileMap = default;
    [SerializeField, Header("NavMeshBuilderを登録")]
    private NavMeshBuilder2D _builder = default;

    [SerializeField, Header("メッシュ生成命令を出すまでの時間")]
    private float _instructionWaitTime = 0.5f;
    /// <summary>
    /// タイル消去を行うメソッド
    /// </summary>
    /// <param name="position">削除するタイルの座標</param>
    public void EraseTile(Vector3 position)
    {
        Vector3Int cellPosition = _tileMap.WorldToCell(position);
        _tileMap.SetTile(cellPosition, null);
        StartCoroutine(REBuildInstructionMethod());
    }

    private IEnumerator REBuildInstructionMethod()
    {
        yield return new WaitForSeconds(_instructionWaitTime);
        _builder.RebuildNavmesh(false);
        Destroy(this.gameObject);

    }
}
