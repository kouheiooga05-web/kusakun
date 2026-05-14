using UnityEngine;

public class GreeningManager : MonoBehaviour
{
    public Terrain terrain;
    [Range(0, 1)] public float globalGreening = 0f;

    public enum LandscapeTier { Desert, Savanna, Grassland, Grove, Forest }
    public LandscapeTier currentTier = LandscapeTier.Desert;

    [Header("環境設定：色とエフェクト")]
    public Color desertFogColor = new Color(0.8f, 0.7f, 0.5f);
    public Color savannaFogColor = new Color(0.7f, 0.8f, 0.6f);
    public Color forestFogColor = new Color(0.4f, 0.6f, 0.5f);

    public GameObject sandStormEffect; // 砂嵐のパーティクルなど
    public GameObject forestLeafEffect; // 落ち葉のパーティクルなど

    private TerrainData terrainData;
    private float[,,] defaultAlphamaps;

    void Start()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
        terrainData = terrain.terrainData;

        // 初期状態を保存
        defaultAlphamaps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

        // 初期の霧を有効にする設定
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
    }

    void Update()
    {
        // 開発中のデバッグ用：インスペクターのglobalGreeningで色が変わるようにする
        UpdateGreening(globalGreening);
    }

    public void UpdateGreening(float percent)
    {
        int width = terrainData.alphamapWidth;
        int height = terrainData.alphamapHeight;
        float[,,] maps = terrainData.GetAlphamaps(0, 0, width, height);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // レイヤー0 (砂) 
                maps[y, x, 0] = 1f - percent;
                // レイヤー1 (草) 
                if (maps.GetLength(2) > 1) // レイヤーが2つ以上あるかチェック
                {
                    maps[y, x, 1] = percent;
                }
            }
        }
        terrainData.SetAlphamaps(0, 0, maps);
    }

    public void SetLandscapeTier(LandscapeTier nextTier)
    {
        currentTier = nextTier;

        switch (currentTier)
        {
            case LandscapeTier.Desert:
                SetEnvironment(1.0f, desertFogColor, sandStormEffect);
                break;
            case LandscapeTier.Savanna:
                SetEnvironment(0.4f, savannaFogColor, null);
                break;
            case LandscapeTier.Forest:
                SetEnvironment(1.0f, forestFogColor, forestLeafEffect);
                break;
        }
    }

    // メソッドの定義はクラス直下（SetLandscapeTierの外）に置く
    private void SetEnvironment(float greenPercent, Color fogColor, GameObject effect)
    {
        // 地面のテクスチャ更新
        globalGreening = greenPercent;
        UpdateGreening(greenPercent);

        // 霧の色の変更
        RenderSettings.fogColor = fogColor;

        // パーティクルの制御（砂嵐を止めたり落ち葉を出したり）
        if (sandStormEffect != null) sandStormEffect.SetActive(currentTier == LandscapeTier.Desert);
        if (forestLeafEffect != null) forestLeafEffect.SetActive(currentTier == LandscapeTier.Forest);

        if (effect != null) effect.SetActive(true);
    }

    void OnApplicationQuit()
    {
        // 終了時にテクスチャを元に戻す
        terrainData.SetAlphamaps(0, 0, defaultAlphamaps);
    }
}