using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{

    //=====初期設定=====
    [SerializeField, Header("移動速度")]
    public float moveSpeed ;
    private CharacterController controller;
    private Vector3 targetPosition; // 目標地点
    private bool isMoving = false;  // 移動中かどうかのフラグ

    private Vector3 mousePressPos;

    //重力（仮）の基準値
    private float verticalVelocity = 0f;
    private readonly float gravity = 9.81f;

    [SerializeField, Header("水分ステータス")]
    public float currentWater;       // 現在の水分量

    public float maxWater = 100f;        // 最大水分量
    public Slider waterSlider;

    public GameObject uiCanvas; // UI陽の設定
    //========================================================


    // ======== 案1：水分消費＆休眠システム用の追加変数 ========
    [Header("サバイバル設定")]
    [SerializeField, Header("1秒間あたりの水分自然減少量")]
    private float waterDrainPerSecond = 2f;

    [SerializeField, Header("休眠からの自動復活時間(秒)")]
    private float recoveryTime = 20f;

    [SerializeField, Header("クリック連打時の1クリックあたりの水分回復量")]
    private float recoveryAmountPerClick = 1.5f;

    private bool isSleeping = false;      // 休眠状態フラグ
    private float sleepTimer = 0f;        // 休眠時間の計測用
    // ========================================================


    void Start()
    { 

        controller = GetComponent<CharacterController>();
        // 最初は自分の位置を目標地点にしておく
        targetPosition = transform.position;

        if (waterSlider != null)
        {
            waterSlider.maxValue = maxWater;
            waterSlider.value = currentWater;
        }
        if (uiCanvas != null){ uiCanvas.SetActive(false);}
    }

    void Update()
    {

        if (isSleeping)
        {
            HandleRecovery();
            return; // 休眠中は以下の移動や通常の水分減少処理をすべてスキップする
        }

        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())//UIと移動の重複防止
        {
            // 移動自体は継続させるが、新しい目的地の上書きは防ぐためにここでReturnはせず、
            // 下のマウス入力だけを制限する形にします。
        }
        else
        {
            // 1. マウス左クリックを検知、目的地設定
            if (Input.GetMouseButtonDown(0)) { mousePressPos = Input.mousePosition; }
            if (Input.GetMouseButtonUp(0))
            {
                if (Vector3.Distance(mousePressPos, Input.mousePosition) < 10f) { SetTargetPosition(); }
            }
        }

        
        if (currentWater > 0f)
        {
            currentWater -= waterDrainPerSecond * Time.deltaTime;
            currentWater = Mathf.Max(currentWater, 0f); // 0未満にならないように固定

            if (waterSlider != null) waterSlider.value = currentWater;

            // 水分が0になった瞬間に休眠状態へ突入
            if (currentWater <= 0f)
            {
                EnterSleepMode();
            }
        }

        // 2. 目標地点に向かって移動
        MoveToTarget();
    }

    // ======== 水分を吸収する公開メソッド（水たまり用） ========
    public void AbsorbWater(float amount)
    {
        // 休眠中は水たまりにいても吸水できない（復活してから吸水させるため）
        if (isSleeping) return;

        currentWater = Mathf.Clamp(currentWater + amount, 0f, maxWater);
        if (waterSlider != null) waterSlider.value = currentWater;
        Debug.Log($"水を吸収中！ 現在の水分量: {currentWater}");
    }

    public bool UseWater(float amount)
    {
        if (currentWater < amount || isSleeping)
        {
            Debug.Log("水分が足りないか、休眠中のため水をあげられません！");
            return false;
        }

        currentWater -= amount;
        if (waterSlider != null) waterSlider.value = currentWater;

        if (currentWater <= 0f)
        {
            EnterSleepMode();
        }

        return true;
    }

    private void EnterSleepMode()
    {
        isSleeping = true;
        isMoving = false; // 移動を強制停止
        sleepTimer = 0f;
        Debug.Log("【休眠】水分が尽きました。草君が丸まっています…（クリック連打か時間経過で復活）");

        if (uiCanvas != null)
        {
            uiCanvas.SetActive(true);

            Button btn = uiCanvas.GetComponentInChildren<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners(); // 二重登録防止
                btn.onClick.AddListener(OnrecoveryButtonClick);
            }

        }

        // 【演出用】ここに「丸まるアニメーション」を再生するトリガーなどを将来入れます
    }

    private void OnrecoveryButtonClick()
    {
        currentWater += recoveryAmountPerClick;
        if (waterSlider != null) waterSlider.value = currentWater;

        Debug.Log($"【ボタン連打復活中】現在の水分: {currentWater}");
    }

    private void HandleRecovery()
    {
        // 1. 時間経過による緩やかなリカバリ
        sleepTimer += Time.deltaTime;


        // 復活条件：一定時間耐えるか、連打で水分がわずか（例: 10以上）に回復したら
        if (sleepTimer >= recoveryTime || currentWater >= 10f)
        {
            isSleeping = false;
            // 復活時は最低限動けるように、水分が0なら少しだけ（5Lほど）ボーナスをあげる
            if (currentWater < 5f) currentWater = 5f;
            if (waterSlider != null) waterSlider.value = currentWater;

            // 目的地を今いる場所にリセット（勝手に歩き出さないように）
            targetPosition = transform.position;

            if (uiCanvas != null) uiCanvas.SetActive(false);

            Debug.Log("【復活！】草君がシャキーンと起き上がりました！");
        }
    }

    void SetTargetPosition() //現状のコードだとスクリプトの衝突が良そう
    {
        // カメラからマウスカーソルの位置に向かってRay（光線）を飛ばす
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        int layerMask = LayerMask.GetMask("Ground");

        // Ground（地面）などのコライダーに当たった場合
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            // 当たった場所の座標をターゲットにする
            targetPosition = hit.point;
            // キャラクターが地面に埋まらないよう、高さ(Y)を自分と同じにする
            targetPosition.y = 0f;

            isMoving = true;

            Debug.Log($"当たったオブジェクト: {hit.collider.gameObject.name} 座標: {hit.point}");
        }
        else
        {
            Debug.Log("何も当たっていません。Layerの設定などを確認してください。");
        }
    }

    void MoveToTarget()
    {

        if (controller.isGrounded)
        {
            // 接地しているときは軽い下向きの力を維持（坂道でのガタつき防止）
            verticalVelocity = -0.5f;
        }
        else
        {
            // 空中にいるときは重力を蓄積していく
            verticalVelocity -= gravity * Time.deltaTime;
        }

        if (!isMoving) return;



        // 目標地点までの方向ベクトルを計算

        Vector3 direction = targetPosition - transform.position;



        // 目的地に十分近ければ停止

        if (direction.magnitude < 0.1f)
        {
            isMoving = false;

            return;

        }
        // 移動の実行（向きを正規化して速度を掛ける）

        Vector3 moveVelocity = direction.normalized * moveSpeed;

        // 計算した水平速度に、重力による垂直速度を合流させる
        moveVelocity.y = verticalVelocity;

        controller.Move(moveVelocity * Time.deltaTime);



        // (おまけ) 移動方向を向かせたい場合はここに追加

        // transform.LookAt(targetPosition);  
    }

}