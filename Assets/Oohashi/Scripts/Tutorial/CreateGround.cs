using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class CreateGround : MonoBehaviour
{
    private Tilemap _tileMap = default;
    [SerializeField, Header("生成するタイル")]
    private TileBase _genTileBases = default;

    private NavMeshBuilder2D _genNavMeshBuilder = default;


    [SerializeField,Header("1列目のタイルマップを生成する座標")]
    private List<Vector2> _firstGenPos = new List<Vector2>();
    [SerializeField,Header("2列目のタイルマップを生成する座標")]
    private List<Vector2> _secondGenPos = new List<Vector2>();

    private void Start()
    {
        GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == 3)
            {
                _tileMap = obj.GetComponent<Tilemap>();
            }

            if(obj.name == "NavBuildMan")
            {
                _genNavMeshBuilder = obj.GetComponent<NavMeshBuilder2D>();
            }
        }
    }

    public void GenerateGroundTile()
    {
        for(int i = 0;  i < _firstGenPos.Count; i++)
        {
            Vector3Int cellPos = _tileMap.WorldToCell(_firstGenPos[i]);
            _tileMap.SetTile(cellPos, _genTileBases);

        }
        for(int j =0; j < _secondGenPos.Count; j++)
        {
            Vector3Int cellPos = _tileMap.WorldToCell(_secondGenPos[j]);
            _tileMap.SetTile(cellPos, _genTileBases);
        }
        Debug.Log("タイルマップ生成完了");
        StartCoroutine(GennavMesh());
    }

    private IEnumerator GennavMesh()
    {
        yield return null;
        Debug.Log("足場生成");
        _genNavMeshBuilder.RebuildNavmesh(false);
    }
}
