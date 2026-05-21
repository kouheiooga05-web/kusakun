using UnityEngine;

public class WaterSource : MonoBehaviour
{
    [Header("1秒あたりに与える水分量")]
    public float waterAmountPerSecond = 20f;

    // そもそも何かが触れた瞬間に呼ばれるかチェック
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[水たまり] 何かが中に入りました: {other.gameObject.name}");
    }


    // トリガー（コライダー）に他オブジェクトが触れている間、ずっと呼び出される
    private void OnTriggerStay(Collider other)
    {
        // 触れてきたオブジェクトに PlayerController が付いているか確認
        PlayerController player = other.GetComponent<PlayerController>();

        if (player != null)
        {
            // 時間（フレーム）に合わせて滑らかに水分を渡す
            player.AbsorbWater(waterAmountPerSecond * Time.deltaTime);
        }
        else
        {
            // PlayerControllerが見つからない場合にログを出す
            Debug.Log($"[水たまり] 接触中ですが、PlayerControllerが見つかりません: {other.gameObject.name}");
        }


    }
}