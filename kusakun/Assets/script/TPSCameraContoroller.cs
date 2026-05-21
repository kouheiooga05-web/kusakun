using UnityEngine;

public class TPSCameraController : MonoBehaviour
{
    [Header("ターゲット設定")]
    public Transform target;        // 草君のTransformをアサイン
    public Vector3 offset = new Vector3(0, 1.0f, 0); // キャラの中央（腰〜胸）を狙う

    [Header("回転設定")]
    public float sensitivity = 5f;  // マウス感度
    public float minVerticalAngle = -10f;
    public float maxVerticalAngle = 60f;

    [Header("ズーム設定")]
    public Transform cameraTransform; // Main Cameraをアサイン
    public float zoomSpeed = 5f;
    public float minDistance = 2f;
    public float maxDistance = 15f;

    [SerializeField, Header("初期ズーム距離")]
    private float defaultDistance = 8f;

    private float currentDistance = 10f; // 初期距離

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        // 初期距離を、設定した「defaultDistance」の範囲内に収めて適用
        currentDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);

        if (cameraTransform != null)
        {
            // ゲーム開始時に、指定された初期距離のポジションにカメラを配置する
            cameraTransform.localPosition = new Vector3(0, cameraTransform.localPosition.y, -currentDistance);
        }
    }


    void Update()
    {
        // マウスホイールの入力を取得 (奥に回すとプラス、手前でマイナス)
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            // 距離を計算して制限（Clamp）をかける
            currentDistance -= scroll * zoomSpeed;
            currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

            // カメラのローカル座標のZ軸を動かす
            cameraTransform.localPosition = new Vector3(0, cameraTransform.localPosition.y, -currentDistance);
        }
    }
    void LateUpdate()
    {
        if (target == null) return;

        // 1. キャラクターの少し上（中心）にカメラの回転軸を合わせる
        transform.position = target.position + offset;

        // 2. 右クリック（または左クリック）ドラッグ中のみ回転させる
        // 0 = 左クリック, 1 = 右クリック
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * sensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

            rotationY += mouseX;
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);
        }

        // 3. 回転を適用
        transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
    }
}