using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル画面などのOptionメニュー内で、FPS設定および垂直同期(VSync)設定のUI表示・操作を行うクラス。
/// 既存のOptionButtons(BGM/SE)からUI要素を自動構築する機能と、Inspectorから個別指定する機能の両方に対応しています。
/// </summary>
public class FrameRateOptionUI : MonoBehaviour
{
    [Header("UI参照（未設定時はOptionButtonsから自動構築）")]
    [SerializeField] private RectTransform _optionButtonsRoot = default;
    [SerializeField] private Text _fpsValueText = default;
    [SerializeField] private Text _vsyncValueText = default;
    [SerializeField] private Button _fpsLeftButton = default;
    [SerializeField] private Button _fpsRightButton = default;
    [SerializeField] private Button _vsyncLeftButton = default;
    [SerializeField] private Button _vsyncRightButton = default;

    [Header("レイアウト自動調整設定")]
    [SerializeField] private bool _autoBuildIfMissing = true;
    [SerializeField] private float _rowScale = 0.72f;
    [SerializeField] private float _firstRowY = 35f;
    [SerializeField] private float _rowSpacing = 115f;

    private SEManager _seManager = default;
    private bool _isBuilt = false;

    private void Awake()
    {
        if (_seManager == null)
        {
            _seManager = FindFirstObjectByType<SEManager>();
        }

        if (_autoBuildIfMissing && (_fpsValueText == null || _vsyncValueText == null))
        {
            BuildOptionRowsFromTemplate();
        }
        else
        {
            BindButtonsIfAssigned();
        }
    }

    private void OnEnable()
    {
        FrameRateManager.OnSettingsChanged += RefreshUI;
        RefreshUI();
    }

    private void OnDisable()
    {
        FrameRateManager.OnSettingsChanged -= RefreshUI;
    }

    private void Start()
    {
        RefreshUI();
    }

    /// <summary>
    /// FPSを次の値へ変更（右ボタン用）
    /// </summary>
    public void OnClickFpsRight()
    {
        FrameRateManager.NextTargetFps();
        PlayClickSE();
        RefreshUI();
    }

    /// <summary>
    /// FPSを前の値へ変更（左ボタン用）
    /// </summary>
    public void OnClickFpsLeft()
    {
        FrameRateManager.PrevTargetFps();
        PlayClickSE();
        RefreshUI();
    }

    /// <summary>
    /// VSyncのON/OFFを切り替え（左右ボタン共通）
    /// </summary>
    public void OnClickVSyncToggle()
    {
        FrameRateManager.ToggleVSync();
        PlayClickSE();
        RefreshUI();
    }

    /// <summary>
    /// 現在の設定値をUIテキストに反映する
    /// </summary>
    public void RefreshUI()
    {
        if (_fpsValueText != null)
        {
            _fpsValueText.text = FrameRateManager.GetTargetFpsDisplayString();
            // VSyncがONの時はモニター同期が優先されるため少し色味を抑えて視覚的に分かりやすくする
            Color c = _fpsValueText.color;
            c.a = FrameRateManager.IsVSyncEnabled ? 0.55f : 1.0f;
            _fpsValueText.color = c;
        }

        if (_vsyncValueText != null)
        {
            _vsyncValueText.text = FrameRateManager.GetVSyncDisplayString();
        }
    }

    private void PlayClickSE()
    {
        if (_seManager == null)
        {
            _seManager = FindFirstObjectByType<SEManager>();
        }
        if (_seManager != null)
        {
            _seManager.PlayEnterSound();
        }
    }

    private void BindButtonsIfAssigned()
    {
        if (_isBuilt) return;
        _isBuilt = true;

        if (_fpsLeftButton != null)
        {
            _fpsLeftButton.onClick.RemoveListener(OnClickFpsLeft);
            _fpsLeftButton.onClick.AddListener(OnClickFpsLeft);
        }
        if (_fpsRightButton != null)
        {
            _fpsRightButton.onClick.RemoveListener(OnClickFpsRight);
            _fpsRightButton.onClick.AddListener(OnClickFpsRight);
        }
        if (_vsyncLeftButton != null)
        {
            _vsyncLeftButton.onClick.RemoveListener(OnClickVSyncToggle);
            _vsyncLeftButton.onClick.AddListener(OnClickVSyncToggle);
        }
        if (_vsyncRightButton != null)
        {
            _vsyncRightButton.onClick.RemoveListener(OnClickVSyncToggle);
            _vsyncRightButton.onClick.AddListener(OnClickVSyncToggle);
        }
    }

    /// <summary>
    /// OptionButtons内の既存要素(BGM, SE, Exit)を元に、FPS設定行とVSync設定行を自動生成・整列する
    /// </summary>
    [ContextMenu("Setup Option UI Elements")]
    public void BuildOptionRowsFromTemplate()
    {
        if (_isBuilt && _fpsValueText != null && _vsyncValueText != null) return;

        if (_optionButtonsRoot == null)
        {
            Transform found = transform.name == "OptionButtons" ? transform : transform.Find("OptionButtons");
            if (found == null && transform.root != null)
            {
                foreach (var rt in transform.root.GetComponentsInChildren<RectTransform>(true))
                {
                    if (rt.name == "OptionButtons")
                    {
                        found = rt;
                        break;
                    }
                }
            }
            if (found != null)
            {
                _optionButtonsRoot = found as RectTransform;
            }
        }

        if (_optionButtonsRoot == null)
        {
            Debug.LogWarning("[FrameRateOptionUI] OptionButtons が見つからないため自動生成をスキップしました。");
            return;
        }

        RectTransform bgmRow = _optionButtonsRoot.Find("BGM") as RectTransform;
        RectTransform seRow = _optionButtonsRoot.Find("SE") as RectTransform;
        RectTransform exitBtn = _optionButtonsRoot.Find("Exit") as RectTransform;

        if (seRow == null)
        {
            Debug.LogWarning("[FrameRateOptionUI] テンプレートとなる SE 行が見つかりません。");
            return;
        }

        // 既に生成済みかチェック
        RectTransform fpsRow = _optionButtonsRoot.Find("FPS") as RectTransform;
        if (fpsRow == null)
        {
            GameObject fpsObj = Instantiate(seRow.gameObject, _optionButtonsRoot);
            fpsObj.name = "FPS";
            fpsRow = fpsObj.GetComponent<RectTransform>();
        }

        RectTransform vsyncRow = _optionButtonsRoot.Find("VSync") as RectTransform;
        if (vsyncRow == null)
        {
            GameObject vsyncObj = Instantiate(seRow.gameObject, _optionButtonsRoot);
            vsyncObj.name = "VSync";
            vsyncRow = vsyncObj.GetComponent<RectTransform>();
        }

        // 4行(BGM, SE, FPS, VSync)が画面内に綺麗に収まるよう位置とスケールを整列
        if (bgmRow != null)
        {
            bgmRow.localScale = new Vector3(_rowScale, _rowScale, 1f);
            bgmRow.anchoredPosition = new Vector2(0f, _firstRowY);
        }

        seRow.localScale = new Vector3(_rowScale, _rowScale, 1f);
        seRow.anchoredPosition = new Vector2(0f, _firstRowY - _rowSpacing);

        fpsRow.localScale = new Vector3(_rowScale, _rowScale, 1f);
        fpsRow.anchoredPosition = new Vector2(0f, _firstRowY - _rowSpacing * 2f);

        vsyncRow.localScale = new Vector3(_rowScale, _rowScale, 1f);
        vsyncRow.anchoredPosition = new Vector2(0f, _firstRowY - _rowSpacing * 3f);

        // Exitボタンを一番下に配置（ヒエラルキー順も最後に移動）
        if (exitBtn != null)
        {
            exitBtn.SetAsLastSibling();
        }

        // FPS行のセットアップ
        SetupClonedRow(
            fpsRow,
            labelTitle: "Frame Rate",
            leftBtnName: "FPSLeft",
            rightBtnName: "FPSRight",
            valueTextName: "FPSSizeText",
            out _fpsLeftButton,
            out _fpsRightButton,
            out _fpsValueText
        );

        // VSync行のセットアップ
        SetupClonedRow(
            vsyncRow,
            labelTitle: "VSync",
            leftBtnName: "VSyncLeft",
            rightBtnName: "VSyncRight",
            valueTextName: "VSyncSizeText",
            out _vsyncLeftButton,
            out _vsyncRightButton,
            out _vsyncValueText
        );

        // クリックイベントの再設定（クローン元のAudioVolumeChangeイベントをクリアして差し替え）
        if (_fpsLeftButton != null)
        {
            _fpsLeftButton.onClick = new Button.ButtonClickedEvent();
            _fpsLeftButton.onClick.AddListener(OnClickFpsLeft);
        }
        if (_fpsRightButton != null)
        {
            _fpsRightButton.onClick = new Button.ButtonClickedEvent();
            _fpsRightButton.onClick.AddListener(OnClickFpsRight);
        }
        if (_vsyncLeftButton != null)
        {
            _vsyncLeftButton.onClick = new Button.ButtonClickedEvent();
            _vsyncLeftButton.onClick.AddListener(OnClickVSyncToggle);
        }
        if (_vsyncRightButton != null)
        {
            _vsyncRightButton.onClick = new Button.ButtonClickedEvent();
            _vsyncRightButton.onClick.AddListener(OnClickVSyncToggle);
        }

        _isBuilt = true;
        RefreshUI();
    }

    private void SetupClonedRow(
        RectTransform row,
        string labelTitle,
        string leftBtnName,
        string rightBtnName,
        string valueTextName,
        out Button leftBtn,
        out Button rightBtn,
        out Text valueText)
    {
        leftBtn = null;
        rightBtn = null;
        valueText = null;

        Button[] buttons = row.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            if (btn.name.Contains("Left"))
            {
                btn.name = leftBtnName;
                leftBtn = btn;
            }
            else if (btn.name.Contains("Right"))
            {
                btn.name = rightBtnName;
                rightBtn = btn;
            }
        }

        Text[] texts = row.GetComponentsInChildren<Text>(true);
        foreach (var txt in texts)
        {
            // ボタンの子テキスト("<", ">")は除外
            if (txt.transform.parent != row) continue;

            if (txt.name.Contains("SizeText") || txt.name.Contains("ValueText"))
            {
                txt.name = valueTextName;
                valueText = txt;
            }
            else if (txt.name.Contains("Text"))
            {
                txt.name = labelTitle.Replace(" ", "") + "Text";
                txt.text = labelTitle;
            }
        }
    }
}
