using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Option メニュー内の FPS・垂直同期(VSync)設定UI。
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

    private bool _isBuilt;

    private void Awake()
    {
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
        UISound.Play(UISoundType.Enter);
        RefreshUI();
    }

    /// <summary>
    /// FPSを前の値へ変更（左ボタン用）
    /// </summary>
    public void OnClickFpsLeft()
    {
        FrameRateManager.PrevTargetFps();
        UISound.Play(UISoundType.Enter);
        RefreshUI();
    }

    /// <summary>
    /// VSyncのON/OFFを切り替え（左右ボタン共通）
    /// </summary>
    public void OnClickVSyncToggle()
    {
        FrameRateManager.ToggleVSync();
        UISound.Play(UISoundType.Enter);
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
            Color c = _fpsValueText.color;
            c.a = FrameRateManager.IsVSyncEnabled ? 0.55f : 1.0f;
            _fpsValueText.color = c;
        }

        if (_vsyncValueText != null)
        {
            _vsyncValueText.text = FrameRateManager.GetVSyncDisplayString();
        }
    }

    private void BindButtonsIfAssigned()
    {
        if (_isBuilt) return;
        _isBuilt = true;

        BindSingleButton(_fpsLeftButton, OnClickFpsLeft, clearExisting: false);
        BindSingleButton(_fpsRightButton, OnClickFpsRight, clearExisting: false);
        BindSingleButton(_vsyncLeftButton, OnClickVSyncToggle, clearExisting: false);
        BindSingleButton(_vsyncRightButton, OnClickVSyncToggle, clearExisting: false);
    }

    private static void BindSingleButton(Button button, UnityEngine.Events.UnityAction action, bool clearExisting)
    {
        if (button == null) return;
        if (clearExisting)
        {
            button.onClick = new Button.ButtonClickedEvent();
        }
        else
        {
            button.onClick.RemoveListener(action);
        }
        button.onClick.AddListener(action);
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
            _optionButtonsRoot = found as RectTransform;
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

        RectTransform fpsRow = EnsureClonedRow("FPS", seRow);
        RectTransform vsyncRow = EnsureClonedRow("VSync", seRow);

        ApplyRowLayout(bgmRow, 0);
        ApplyRowLayout(seRow, 1);
        ApplyRowLayout(fpsRow, 2);
        ApplyRowLayout(vsyncRow, 3);

        if (exitBtn != null)
        {
            exitBtn.SetAsLastSibling();
        }

        SetupClonedRow(
            fpsRow,
            labelTitle: "Frame Rate",
            leftBtnName: "FPSLeft",
            rightBtnName: "FPSRight",
            valueTextName: "FPSSizeText",
            out _fpsLeftButton,
            out _fpsRightButton,
            out _fpsValueText);

        SetupClonedRow(
            vsyncRow,
            labelTitle: "VSync",
            leftBtnName: "VSyncLeft",
            rightBtnName: "VSyncRight",
            valueTextName: "VSyncSizeText",
            out _vsyncLeftButton,
            out _vsyncRightButton,
            out _vsyncValueText);

        BindSingleButton(_fpsLeftButton, OnClickFpsLeft, clearExisting: true);
        BindSingleButton(_fpsRightButton, OnClickFpsRight, clearExisting: true);
        BindSingleButton(_vsyncLeftButton, OnClickVSyncToggle, clearExisting: true);
        BindSingleButton(_vsyncRightButton, OnClickVSyncToggle, clearExisting: true);

        _isBuilt = true;
        RefreshUI();
    }

    private RectTransform EnsureClonedRow(string rowName, RectTransform templateRow)
    {
        RectTransform existing = _optionButtonsRoot.Find(rowName) as RectTransform;
        if (existing != null) return existing;

        GameObject cloned = Instantiate(templateRow.gameObject, _optionButtonsRoot);
        cloned.name = rowName;
        return cloned.GetComponent<RectTransform>();
    }

    private void ApplyRowLayout(RectTransform row, int rowIndex)
    {
        if (row == null) return;
        row.localScale = new Vector3(_rowScale, _rowScale, 1f);
        row.anchoredPosition = new Vector2(0f, _firstRowY - _rowSpacing * rowIndex);
    }

    private static void SetupClonedRow(
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
        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
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
        for (int i = 0; i < texts.Length; i++)
        {
            var txt = texts[i];
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
