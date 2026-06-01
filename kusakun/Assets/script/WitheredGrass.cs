using UnityEngine;
using UnityEngine.UI;

public class WitheredGrass : MonoBehaviour
{
    [Header("参照設定")]
    // インスペクターで、子オブジェクトにあるCanvas(WitheredGrassUI)をドラッグ＆ドロップする
    public GameObject uiCanvas;
    public float giveWaterAmount = 30f;

    // 【追加】枯草の2Dイラスト（SpriteRendererなどが入った子オブジェクト）
    [Header("枯草の見た目オブジェクト")]
    public Transform grassVisual;

    private PlayerController playerInArea;

    // ======== 仲間集合システム用の追加変数 ========
    [Header("緑化・集合設定")]
    [SerializeField, Header("元気になった時のSprite（緑の草）")]
    private Sprite greenGrassSprite;

    [SerializeField, Header("水辺へ移動する速度")]
    private float moveSpeedToWater = 2.0f;

    private bool isGreened = false;       // 緑化したかどうかのフラグ
    private Vector3 waterPosition;        // 目標とする水たまりの座標
    private SpriteRenderer spriteRenderer; // 見た目を切り替える用
    // ========================================================

    void Start()
    {
        // 最初はボタン（UI）を非表示にしておく
        if (uiCanvas != null) uiCanvas.SetActive(false);
        if (grassVisual != null)
        {
            spriteRenderer = grassVisual.GetComponent<SpriteRenderer>();
        }
    }

    void Update()
    {
        // 緑化して、かつ水たまりの位置がセットされている場合のみ移動する
        if (isGreened)
        {
            // 水たまりへの方向と距離を計算
            Vector3 direction = waterPosition - transform.position;

            // まだ水たまりに到着していない場合（距離が0.5メートル以上離れている場合）
            if (direction.magnitude > 0.5f)
            {
                // 水たまりに向かって少しずつ座標を移動させる
                transform.position += direction.normalized * moveSpeedToWater * Time.deltaTime;
            }
            else
            {
                // 到着したら移動処理を終了する
                Debug.Log($"{gameObject.name} が無事に水辺に到着しました！");
                enabled = false;
            }
        }
    }

    // プレイヤーが近づいた
    private void OnTriggerEnter(Collider other)
    {

        if (isGreened) return;

        PlayerController player = other.GetComponent<PlayerController>();

        if (player == null && (other.CompareTag("Player") || other.name.Contains("Player")))
        {
            player = GameObject.FindWithTag("Player")?.GetComponent<PlayerController>();
        }

        if (player != null)
        {
            playerInArea = player;

            // 頭上のUIを表示する
            if (uiCanvas != null)
            {
                uiCanvas.SetActive(true);

                // ボタンのクリックイベントに処理を登録
                Button btn = uiCanvas.GetComponentInChildren<Button>();
                if (btn != null)
                {
                    // 以前のリスナーが残っていると重複するので一回クリアして登録
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(OnWaterButtonClick);
                }
            }
        }
    }

    // プレイヤーが離れた
    private void OnTriggerExit(Collider other)
    {
        if (isGreened) return;
        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player != null || other.CompareTag("Player"))
        {
            playerInArea = null;
            if (uiCanvas != null) uiCanvas.SetActive(false);
        }
    }

    private void OnWaterButtonClick()
    {
        if (playerInArea != null)
        {
            bool success = playerInArea.UseWater(giveWaterAmount);
            if (success)
            {
                ProceedGreening();
            }
        }
    }

    private void ProceedGreening()
    {
        Debug.Log("枯草が緑化しました！");
        isGreened = true;
        // 【演出】ここに2Dイラストを「緑色の草」のSpriteに差し替える処理などを入れる
        // 1. UIを非表示にし、接近判定用のコライダーを消す（これ以上水をあげられないように）
        if (uiCanvas != null) uiCanvas.SetActive(false);
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 2. 見た目を「緑の草」に差し替える
        if (spriteRenderer != null && greenGrassSprite != null)
        {
            spriteRenderer.sprite = greenGrassSprite;
        }

        // 3. 「Water」タグが付いた水たまりの座標を探して記憶する
        GameObject waterObj = GameObject.FindWithTag("Water");
        if (waterObj != null)
        {
            waterPosition = waterObj.transform.position;
            // 仲間が地面に埋まったり浮いたりしないよう、高さ（Y軸）は自分の現在の高さをキープ
            waterPosition.y = transform.position.y;
        }
        else
        {
            // もしタグが見つからなかった場合の安全策として原点(0,0,0)を目指す
            waterPosition = Vector3.zero;
            Debug.LogWarning("Waterタグが付いたオブジェクトが見つかりません！インスペクターのTagを確認してください。");
        }
    }

    void LateUpdate()
    {
        if (Camera.main == null) return;

        // カメラの回転情報を取得
        Quaternion cameraRotation = Camera.main.transform.rotation;

        // 1. UI Canvas をカメラの正面に向かせる
        if (uiCanvas != null && uiCanvas.activeSelf)
        {
            uiCanvas.transform.LookAt(uiCanvas.transform.position + cameraRotation * Vector3.forward, cameraRotation * Vector3.up);
        }

        // 2. 枯草のイラスト（Sprite）もカメラの正面に向かせる
        if (grassVisual != null)
        {
            grassVisual.LookAt(grassVisual.transform.position + cameraRotation * Vector3.forward, cameraRotation * Vector3.up);
        }
    }
}