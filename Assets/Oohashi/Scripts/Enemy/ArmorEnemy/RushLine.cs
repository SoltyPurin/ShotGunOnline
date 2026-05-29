using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RushLine : MonoBehaviour
{

    [SerializeField, Header("突進線のオブジェクト")]
    private GameObject _rushLineObj = default;

    [SerializeField, Header("移動スクリプト")]
    private ArmorMove _armorMove = default;

    [SerializeField, Header("回転スクリプト")]
    private ArmorRotation _armorRotation = default;

    [SerializeField, Header("塗りつぶしのマテリアル")]
    private Material _innerMaterial = default;

    [SerializeField, Header("メッシュフィルター")]
    private MeshFilter _meshFilter = default;

    [SerializeField, Header("メッシュ")]
    private Mesh _innerMesh = default;

    [SerializeField, Header("メッシュレンダラー")]
    private MeshRenderer _meshRenderer = default;

    [SerializeField, Header("横幅")]
    private float _width = 2.0f;

    private readonly string PLAYER_TAGNAME = "Player";

    private Vector3[] _vertices = new Vector3[4];

    private int[] _triangles = new int[6] { 0, 2, 1,
                                            2, 3, 1   };

    private Vector3 _prevPos = default;

    private void Awake()
    {
        //最初にメッシュを生成する
        _innerMesh = new Mesh();

        _meshFilter.mesh = _innerMesh;

        _rushLineObj.SetActive(false);
    }
    /// <summary>
    /// 突撃範囲を表示する
    /// </summary>
    private void FixedUpdate()
    {
        //落下したらそもそも表示しない
        if(_armorMove.EnemyState == EnemyState.fall)
        {
            _meshRenderer.enabled = false;
            return;
        }
        else
        {
            _meshRenderer.enabled = true;
        }
            float range = _armorMove.MoveSpeed * _armorMove.RushTime;

        switch (_armorMove.InitState)
        {
            case ArmorState.Stop:
                transform.localPosition = Vector3.zero;
                _rushLineObj.transform.localPosition = Vector3.zero;
                _meshRenderer.enabled = false;
                _rushLineObj.SetActive(false);
                break;

            case ArmorState.Rotate:

                _rushLineObj.SetActive(true);

                //_rushLineObj.transform.rotation = _armorRotation.RotateDirection;

                //Quaternion rotation = _armorRotation.RotateDirection;

                //Vector3 localLeftBottom = rotation * new Vector3(-_width / 2, 0, 0);
                //Vector3 localRightBottom = rotation * new Vector3(_width / 2, 0, 0);
                //Vector3 localRightTop = rotation * new Vector3(_width / 2, range, 0);
                //Vector3 localLeftTop = rotation * new Vector3(-_width / 2, range, 0);

                //_vertices[0] = localLeftBottom;
                //_vertices[1] = localRightBottom;
                //_vertices[2] = localLeftTop;
                //_vertices[3] = localRightTop;

                break;

            case ArmorState.Reservoir:
                _prevPos = this.transform.position;
                break;

            case ArmorState.Rush:
                //_rushLineObj.transform.position = _prevPos;

                break;
        }




        _innerMesh.vertices = _vertices;

        _innerMesh.subMeshCount = 1;

        _innerMesh.SetTriangles(_triangles, 0);

        _innerMesh.uv = new Vector2[]
        {
             new Vector2(0, 0),
             new Vector2(1, 0),
             new Vector2(1, 1),
             new Vector2(0, 1),
        };

        _meshRenderer.sharedMaterial = _innerMaterial;

        _meshFilter.mesh = _innerMesh;

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        for (int i = 0; i < _vertices.Length; i++)
        {
            Gizmos.DrawSphere(transform.position + _vertices[i], 0.1f);
        }
    }
}
