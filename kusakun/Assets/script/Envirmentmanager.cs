using UnityEngine;

public class GreeningManager : MonoBehaviour
{
    public static GreeningManager Instance { get; private set; }

    [System.Serializable]
    public enum EnvironmentStage
    {
        Desert,   // 0: 砂漠
        Savanna,  // 1: サバンナ
        Grassland,// 2: 草原
        Grove,    // 3: 林
        Forest    // 4: 森
    }

    [Header("環境ステータス")]
    public EnvironmentStage currentStage = EnvironmentStage.Desert;

    [Header("救出カウント")]
    public int rescuedCountInCurrentStage = 0; // 現在の階層での救出数
    public int totalRescuedCount = 0;          // ゲーム全体の総救出数

    [Header("次の階層へ進むために必要な救出しきい値")]
    [SerializeField] private int requiredRescueForSavanna = 2;   // 砂漠→サバンナに必要な数
    [SerializeField] private int requiredRescueForGrassland = 3; // サバンナ→草原に必要な数
    [SerializeField] private int requiredRescueForGrove = 5;     // 草原→林に必要な数
    [SerializeField] private int requiredRescueForForest = 8;    // 林→森に必要な数

    [Header("対象のTerrain（地面の高さを取る用）")]
    public Terrain targetTerrain;

    [Header("生成する植物のPrefab設定")]
    // インスペクターで、生やしたい3Dの草や2Dビルボード草のPrefabをセットする
    [SerializeField] private GameObject lowGrassPrefab;  // 背の低い草（サバンナ・草原用）
    [SerializeField] private GameObject tallGrassPrefab; // 背の高い草・花（草原・林用）
    [SerializeField] private GameObject bushPrefab;      // 低木・茂み（林・森用）

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    /// <summary>
    /// 枯草が救出されたら、WitheredGrassから必ず呼び出される関数
    /// </summary>
    public void OnGrassRescued(Vector3 eventPosition)
    {
        rescuedCountInCurrentStage++;
        totalRescuedCount++;

        Debug.Log($"[救出報告] 仲間が救われました！ 現在の階層での救出数: {rescuedCountInCurrentStage}");

        // 1. まずは、救出した仲間の周りにその階層に応じた植物をポコポコ生やす（アプローチA）
        SpawnPlantsAtEvent(eventPosition);

        // 2. 救出数が目標に達しているかチェックし、達していれば環境をアップグレードする
        CheckStageProgression(eventPosition);
    }

    /// <summary>
    /// 救出数に応じて環境の階層（ステージ）を進めるか判定する
    /// </summary>
    private void CheckStageProgression(Vector3 lastEventPos)
    {
        bool didAdvance = false;

        switch (currentStage)
        {
            case EnvironmentStage.Desert:
                if (rescuedCountInCurrentStage >= requiredRescueForSavanna)
                {
                    currentStage = EnvironmentStage.Savanna;
                    didAdvance = true;
                }
                break;

            case EnvironmentStage.Savanna:
                if (rescuedCountInCurrentStage >= requiredRescueForGrassland)
                {
                    currentStage = EnvironmentStage.Grassland;
                    didAdvance = true;
                }
                break;

            case EnvironmentStage.Grassland:
                if (rescuedCountInCurrentStage >= requiredRescueForGrove)
                {
                    currentStage = EnvironmentStage.Grove;
                    didAdvance = true;
                }
                break;

            case EnvironmentStage.Grove:
                if (rescuedCountInCurrentStage >= requiredRescueForForest)
                {
                    currentStage = EnvironmentStage.Forest;
                    didAdvance = true;
                }
                break;

            case EnvironmentStage.Forest:
                // すでに最終形態の「森」
                break;
        }

        if (didAdvance)
        {
            // 階層が上がったので、現在の階層でのカウントをリセット
            rescuedCountInCurrentStage = 0;

            Debug.Log($"<color=green>【環境進化！】世界が次のフェーズに移りました。現在の環境: {currentStage}</color>");

            // 地面のテクスチャを大きく変える処理などをここで連動
            ApplyTerrainTextureChange();
        }
    }

    /// <summary>
    /// 救出地点の周りに、現在の環境レベルに合わせた植物Prefabをランダム生成する
    /// </summary>
    private void SpawnPlantsAtEvent(Vector3 centerPos)
    {
        int spawnCount = 0;
        float radius = 3f;
        GameObject prefabToSpawn = null;

        // 現在のステージに応じて、生やす草の種類と量を変える
        switch (currentStage)
        {
            case EnvironmentStage.Desert:
                spawnCount = 5;
                radius = 2f;
                prefabToSpawn = lowGrassPrefab; // 砂漠ではまだショボショボした草
                break;

            case EnvironmentStage.Savanna:
                spawnCount = 10;
                radius = 4f;
                prefabToSpawn = lowGrassPrefab;
                break;

            case EnvironmentStage.Grassland:
                spawnCount = 15;
                radius = 5f;
                prefabToSpawn = tallGrassPrefab; // 草原からは背の高い草や花が混じる
                break;

            case EnvironmentStage.Grove:
                spawnCount = 20;
                radius = 6f;
                prefabToSpawn = bushPrefab; // 林からは茂みなども発生
                break;

            case EnvironmentStage.Forest:
                spawnCount = 30;
                radius = 8f;
                prefabToSpawn = bushPrefab;
                break;
        }

        if (prefabToSpawn == null) return;

        // 指定数だけPrefabをクローン生成
        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            Vector3 spawnPos = centerPos + new Vector3(randomCircle.x, 0f, randomCircle.y);

            // Terrainの正確な高さを取得して合わせる
            if (targetTerrain != null)
            {
                spawnPos.y = targetTerrain.SampleHeight(spawnPos) + targetTerrain.transform.position.y;
            }

            // ランダムなY軸回転を与えて自然な見た目にする
            Quaternion randomRot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            // 生成！
            Instantiate(prefabToSpawn, spawnPos, randomRot);
        }
    }

    /// <summary>
    /// 環境が切り替わったときにTerrainの見た目（全体）に変化を与える（仮実装）
    /// </summary>
    private void ApplyTerrainTextureChange()
    {
        // ここにTerrainのAlphamapを変更するコード、
        // または環境の「空（Skybox）」や「環境光」を変える処理を入れると一気に豪華になります！
        Debug.Log($"[見た目変化] 地面が {currentStage} のテクスチャに書き換わりました。");
    }
}