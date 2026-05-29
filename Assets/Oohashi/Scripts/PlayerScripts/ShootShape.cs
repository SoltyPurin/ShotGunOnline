using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;

public class ShootShape : MonoBehaviour
{

    [SerializeField, Header("ショットガンのオブジェクト")]
    private GameObject _gunBurrel = default;

    [SerializeField,Header("ラインレンダラー")] 
    private LineRenderer _lenderer = default;

    [SerializeField,Header("ショットの当たり判定のスクリプト")]
    private ShootRange _shootRange = default;

    [SerializeField,Header("プレイヤーの射撃入力スクリプト")]
    private InputPlayerShot _playerShot = default;

    [SerializeField, Header("プレイヤーのステート")]
    private PlayerStateManager _playerState = default;

    [SerializeField, Header("オバヒ取得用のスクリプト")]
    private OverHeat _overHeat = default;

    [SerializeField, Header("塗りつぶしのマテリアル")]
    private Material _innerMaterial = default;

    [SerializeField, Header("メッシュフィルター")]
    private MeshFilter _meshFilter = default;

    [SerializeField,Header("メッシュ")]
    private Mesh _innerMesh = default;

    [SerializeField,Header("メッシュレンダラー")]
    private MeshRenderer _meshRenderer = default;

    [SerializeField,Header("光るアニメーター")]
    private Animator _animator = default;

    //最大チャージ一回だけを判定
    private bool _isOnceMaterialFlash = false;

    private Vector3[] _vertices = new Vector3[3];

    private int[] _triangles = new int[3] { 0, 1, 2 };

    private const float MAXBLUEVALUE = 242;

    private const float MAXGREENVALUE = 85;

    //なにも入力してないときの見てる方向
    private Vector3 _notShootTimeDirection = default;
    public Vector3 NotShootTimeDirection
    {
        set { _notShootTimeDirection = value; }
    }

    //表示する横幅
    private float _width = default;

    private Vector3 _leftTop = default;
    public Vector3 LeftTop
    {
        get { return _leftTop; }
    }

    private Vector3 _rightTop = default;
    public Vector3 RightTop
    {
        get { return _rightTop; }
    }

    private Vector3 _bottom = default;
    public Vector3 Bottom
    {
        get { return _bottom; }
    }



    private void Awake()
    {
        _innerMesh = new Mesh();
    }


    private void Update()
    {
        if(_playerState.PlayerState == PlayerState.Ultimate)
        {
            return; //ウルト状態だったら処理しない
        }

        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, _gunBurrel.transform.right);

        float blueValue = Mathf.Lerp(MAXBLUEVALUE, 0, _overHeat.ComplementValue);
        float greenValue = Mathf.Lerp(MAXGREENVALUE, 0, _overHeat.ComplementValue);

        _lenderer.material.color = new Color32(255, (byte)blueValue, (byte)greenValue, 255);


        _lenderer.startWidth = 0.1f;//線の幅
        _lenderer.endWidth = 0.1f;//線の幅
        //現在のチャージ時間を割って0,1で出力、補正値とする
        float t = Mathf.Clamp01(_playerShot.ChargeValue / 2f);
        //射程距離を代入
        float radius = Mathf.Lerp(_shootRange.MinRange, _shootRange.MaxRange, t);
        //現在の最大角度を代入
        float maxAngle = Mathf.Lerp(_shootRange.MaxAngle, _shootRange.MinAngle, t);
        //最大チャージではないかのフラグ
        bool isNotMaxCharge = (_playerShot.ChargeValue<  2);
        if (isNotMaxCharge)
        {
            if (_isOnceMaterialFlash)
            {
                _isOnceMaterialFlash = false;
                _animator.SetTrigger("Return");
            }
            //徐々に横幅を変える
            _width = radius * Mathf.Tan(Mathf.Deg2Rad * (maxAngle / 2));
        }
        else
        {
            if (!_isOnceMaterialFlash)
            {
                _isOnceMaterialFlash = true;
                _animator.SetTrigger("Flash");
            }
            //大小のピンポン
            _width = radius * Mathf.Tan(Mathf.Deg2Rad * (_shootRange.ViewAngle / 2));
        }


        _lenderer.positionCount = 4; //頂点の数

        _bottom = Vector3.zero;//下
        _leftTop = new Vector3(-_width, radius, 0);//左上
        _rightTop= new Vector3(_width, radius, 0);//右上
        //プレイヤーの向いてる方に回転
        //Quaternion rotation = Quaternion.FromToRotation(Vector3.up, _notShootTimeDirection);

        _bottom = rotation * _bottom + transform.position;
        _leftTop = rotation * _leftTop + transform.position;
        _rightTop = rotation * _rightTop + transform.position;

        _lenderer.SetPosition(0, _bottom);
        _lenderer.SetPosition(1, _leftTop);
        _lenderer.SetPosition(2, _rightTop);
        _lenderer.SetPosition(3, _bottom);// 戻る


        Vector3 localBottom = rotation * Vector3.zero;
        Vector3 localLeftTop = rotation * new Vector3(-_width/2 , radius/2, 0);
        Vector3 localRightTop = rotation * new Vector3(_width/2 , radius/2, 0);

        _vertices[0] = localBottom;
        _vertices[1] = localLeftTop;
        _vertices[2] = localRightTop;


        _innerMesh.vertices = _vertices;

        _innerMesh.subMeshCount = 1;

        _innerMesh.SetTriangles(_triangles, 0);

        _innerMesh.uv = new Vector2[]
        {
            _vertices[0],
            _vertices[1],
            _vertices[2],
        };

        _meshRenderer.sharedMaterial = _innerMaterial;

        _meshFilter.sharedMesh = _innerMesh;

    }

    public void UltShape(Vector3 direction)
    {
        //プレイヤーの向いてる方に回転
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, _gunBurrel.transform.right);

        _lenderer.startWidth = 0.1f;//線の幅
        _lenderer.endWidth = 0.1f;//線の幅
        float radius = 15;       // 射程固定
        float maxAngle = 80f;     // 前方90度以内だけ
        float width = radius * Mathf.Tan(Mathf.Deg2Rad * (maxAngle / 2));


        _lenderer.positionCount = 4; //頂点の数、終点と始点をつなげるために4個用意

        Vector3 bottom = new Vector3(0, 0, 0);//左下
        Vector3 leftTop = new Vector3(-width, radius, 0);//左上
        Vector3 rightTop = new Vector3(width, radius, 0);//右上

        bottom = rotation * bottom + transform.position;
        leftTop = rotation * leftTop + transform.position;
        rightTop = rotation * rightTop + transform.position;

        _lenderer.SetPosition(0, bottom);
        _lenderer.SetPosition(1, leftTop);
        _lenderer.SetPosition(2, rightTop);
        _lenderer.SetPosition(3, bottom); // 戻る

        Vector3 localBottom = rotation * Vector3.zero;
        Vector3 localLeftTop = rotation * new Vector3(-width / 2, radius / 2, 0);
        Vector3 localRightTop = rotation * new Vector3(width / 2, radius / 2, 0);

        _vertices[0] = localBottom;
        _vertices[1] = localLeftTop;
        _vertices[2] = localRightTop;


        _innerMesh.vertices = _vertices;

        _innerMesh.subMeshCount = 1;

        _innerMesh.SetTriangles(_triangles, 0);

        _innerMesh.uv = new Vector2[]
        {
            _vertices[0],
            _vertices[1],
            _vertices[2]
        };

        _meshRenderer.sharedMaterial = _innerMaterial;

        _meshFilter.sharedMesh = _innerMesh;


    }
}
