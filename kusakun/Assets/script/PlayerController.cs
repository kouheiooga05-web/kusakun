using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField, Header("移動速度")]
    public float moveSpeed ;
    private CharacterController controller;
    private Vector3 targetPosition; // 目標地点
    private bool isMoving = false;  // 移動中かどうかのフラグ

    private Vector3 mousePressPos;

    //重力（仮）の基準値
    private float verticalVelocity = 0f;
    private readonly float gravity = 9.81f;

    [Header("水分ステータス")]
    public float currentWater = 0f;       // 現在の水分量
    public float maxWater = 100f;        // 最大水分量
    public Slider waterSlider;

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
    }

    void Update()
    {
        // 1. マウス左クリックを検知
        if (Input.GetMouseButtonDown(0))
        {
            mousePressPos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (Vector3.Distance(mousePressPos, Input.mousePosition) < 10f)
            {
                SetTargetPosition();
            }
        }

        // 2. 目標地点に向かって移動
        MoveToTarget();
    }

    public void AbsorbWater(float amount)//水分量の管理
    {
        // 水分を増加させ、最大値(maxWater)を超えないように制限する
        currentWater = Mathf.Clamp(currentWater + amount, 0f, maxWater);

        // UIのメーターに反映
        if (waterSlider != null)
        {
            waterSlider.value = currentWater;
        }

        Debug.Log($"水を吸収中！ 現在の水分量: {currentWater}");
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