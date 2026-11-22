using System.Collections;
using UnityEngine;

// 챕터별 배경을 보여주고, 스테이지 줌아웃에 맞춰 배경도 함께 줌아웃한다.
//
// 배율 규칙 (줌아웃 한 번에 2배씩)
//   챕터의 마지막 스테이지에서 배경 전체가 보인다(배율 1).
//   그 전 스테이지는 2배, 그 전은 4배로 확대된 부분부터 시작한다.
//   예) 1~3스테이지: 4배 → 2배 → 1배 / 7~8스테이지: 2배 → 1배
//
// 확대 기준점은 StageManager의 축소 기준점(패들 윗면, 화면 가운데)과 같다.
// 그래서 벽돌이 줄어드는 모습과 배경이 줄어드는 모습이 정확히 맞물린다.
[RequireComponent(typeof(SpriteRenderer))]
public class ChapterBackground : MonoBehaviour
{
    // 0: 1챕터(방), 1: 2챕터(도시), 2: 3챕터(우주)
    [SerializeField] private Sprite[] chapterSprites;

    // 각 챕터가 시작되는 스테이지 번호 (1~3, 4~6, 7~8)
    [SerializeField] private int[] chapterStartStages = { 1, 4, 7 };

    // 마지막 스테이지 번호 (마지막 챕터가 어디서 끝나는지 계산할 때 사용)
    [SerializeField] private int finalStage = 8;

    // 줌아웃 한 번의 배율 (StageManager의 StageShrinkRatio 0.5와 짝)
    private const float ZoomStep = 2f;

    private SpriteRenderer mainRenderer;  // 현재 챕터 배경
    private SpriteRenderer fadeRenderer;  // 챕터가 바뀔 때 사라지는 이전 챕터 배경
    private GameManager gameManager;
    private Camera targetCamera;

    private float mainZoom = 1f;
    private float fadeZoom = 1f;
    private Vector3 pivot;
    private Coroutine running;

    void Awake()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
        gameManager = FindFirstObjectByType<GameManager>();
        targetCamera = Camera.main;

        // 이전 챕터 배경용 렌더러를 하나 더 만든다. 현재 배경보다 한 칸 앞에 그린다.
        GameObject fadeObject = new GameObject("Background_PrevChapter");
        fadeRenderer = fadeObject.AddComponent<SpriteRenderer>();
        fadeRenderer.sharedMaterial = mainRenderer.sharedMaterial;
        fadeRenderer.sortingLayerID = mainRenderer.sortingLayerID;
        fadeRenderer.sortingOrder = mainRenderer.sortingOrder + 1;
        fadeRenderer.enabled = false;
    }

    void Start()
    {
        pivot = FindDefaultPivot();
        int stage = gameManager != null ? gameManager.currentStage : 1;
        mainRenderer.sprite = GetSprite(stage);
        mainZoom = GetZoom(stage);
    }

    void LateUpdate()
    {
        Layout(mainRenderer, mainZoom);
        if (fadeRenderer.enabled)
        {
            Layout(fadeRenderer, fadeZoom);
        }
    }

    // StageManager가 스테이지 전환을 시작할 때 호출한다.
    // nextStage: 전환 후 스테이지 번호, shrinkPivot: 축소 기준점, duration: 전환 시간
    public void PlayZoomOut(int nextStage, Vector3 shrinkPivot, float duration)
    {
        pivot = shrinkPivot;
        if (running != null)
        {
            StopCoroutine(running);
        }
        running = StartCoroutine(ZoomRoutine(nextStage, duration));
    }

    IEnumerator ZoomRoutine(int nextStage, float duration)
    {
        float startZoom = mainZoom;
        float endZoom = GetZoom(nextStage);
        bool chapterChanged = GetChapter(nextStage) != GetChapter(nextStage - 1);

        if (chapterChanged)
        {
            // [임시 연출] 이전 챕터 배경은 앞에서 계속 줄어들며 사라지고,
            // 새 챕터 배경은 뒤에서 2배 확대 상태부터 같이 줄어든다.
            fadeRenderer.sprite = mainRenderer.sprite;
            fadeRenderer.color = Color.white;
            fadeRenderer.enabled = true;
            fadeZoom = startZoom;

            mainRenderer.sprite = GetSprite(nextStage);
            startZoom = endZoom * ZoomStep;
        }

        float fadeStartZoom = fadeZoom;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            // StageManager와 같은 선형 보간이라 벽돌과 같은 속도로 줄어든다.
            mainZoom = Mathf.Lerp(startZoom, endZoom, t);

            if (chapterChanged)
            {
                fadeZoom = Mathf.Lerp(fadeStartZoom, fadeStartZoom / ZoomStep, t);
                fadeRenderer.color = new Color(1f, 1f, 1f, 1f - t);
            }
            yield return null;
        }

        mainZoom = endZoom;
        fadeRenderer.enabled = false;
        running = null;
    }

    // 스테이지 번호로 챕터 순서(0부터)를 구한다.
    public int GetChapter(int stage)
    {
        int chapter = 0;
        for (int i = 0; i < chapterStartStages.Length; i++)
        {
            if (stage >= chapterStartStages[i])
            {
                chapter = i;
            }
        }
        return chapter;
    }

    // 해당 스테이지에서 배경 배율. 챕터 마지막 스테이지가 1배.
    public float GetZoom(int stage)
    {
        int chapter = GetChapter(stage);
        int chapterEnd = chapter + 1 < chapterStartStages.Length
            ? chapterStartStages[chapter + 1] - 1
            : finalStage;
        int stepsLeft = Mathf.Max(0, chapterEnd - stage);
        return Mathf.Pow(ZoomStep, stepsLeft);
    }

    Sprite GetSprite(int stage)
    {
        int chapter = GetChapter(stage);
        if (chapterSprites == null || chapter >= chapterSprites.Length)
        {
            return mainRenderer.sprite;
        }
        return chapterSprites[chapter];
    }

    // 첫 전환 전에도 같은 기준점을 쓰도록 패들 윗면 높이를 찾는다.
    Vector3 FindDefaultPivot()
    {
        PaddleMove paddle = FindFirstObjectByType<PaddleMove>();
        if (paddle != null)
        {
            Collider2D col = paddle.GetComponent<Collider2D>();
            if (col != null)
            {
                return new Vector3(0f, col.bounds.max.y, 0f);
            }
        }
        return Vector3.zero;
    }

    // 배율 1일 때 화면을 빈틈없이 덮고, 배율만큼 기준점을 중심으로 확대한다.
    void Layout(SpriteRenderer target, float zoom)
    {
        if (targetCamera == null || target.sprite == null)
        {
            return;
        }

        float screenHeight = targetCamera.orthographicSize * 2f;
        float screenWidth = screenHeight * targetCamera.aspect;
        Vector2 spriteSize = target.sprite.bounds.size;
        float coverScale = Mathf.Max(screenWidth / spriteSize.x, screenHeight / spriteSize.y);

        float scale = coverScale * zoom;
        target.transform.localScale = new Vector3(scale, scale, 1f);

        // 화면 가운데에 놓인 배경을 기준점 중심으로 zoom배 확대한 위치
        Vector3 cam = targetCamera.transform.position;
        float x = pivot.x + (cam.x - pivot.x) * zoom;
        float y = pivot.y + (cam.y - pivot.y) * zoom;
        target.transform.position = new Vector3(x, y, transform.position.z);
    }
}
