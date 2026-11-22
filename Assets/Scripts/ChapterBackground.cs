using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 챕터별 배경을 보여주고, 스테이지 줌아웃에 맞춰 배경도 함께 줌아웃한다.
//
// [챕터 안에서]
//   줌아웃 한 번에 2배씩 멀어지고, 챕터 마지막 스테이지에서 배경 전체가 보인다.
//   예) 1~3, 4~6, 7~9스테이지 모두 4배 → 2배 → 1배
//   확대 기준점은 StageManager의 축소 기준점(패들 윗면, 화면 가운데)이다.
//   각 챕터 그림에서 이 기준점에 놓일 지점을 Anchor(0~1 비율)로 정한다.
//
// [챕터가 바뀔 때]
//   새 챕터에 Portal(창문) 영역이 있으면, 이전 챕터 그림이 그 창문 안으로 빨려 들어가며 줄어든다.
//   창문 안에는 이전 그림이 잘린 채 흐리게 남고, 다음 스테이지로 넘어가면 완전히 사라진다.
//   Portal이 없으면 이전 그림이 줄어들며 흐려지는 기본 연출을 쓴다.
[RequireComponent(typeof(SpriteRenderer))]
public class ChapterBackground : MonoBehaviour
{
    // 0: 1챕터(방), 1: 2챕터(도시), 2: 3챕터(우주)
    [SerializeField] private Sprite[] chapterSprites;

    // 각 챕터가 시작되는 스테이지 번호 (1~3, 4~6, 7~9)
    [SerializeField] private int[] chapterStartStages = { 1, 4, 7 };

    // 챕터 그림에서 확대 기준점에 놓일 지점 (왼쪽 아래 0,0 ~ 오른쪽 위 1,1)
    [SerializeField] private Vector2[] chapterAnchors =
    {
        new Vector2(0.5f, 0.15f),       // 방
        new Vector2(0.5388f, 0.1534f),  // 도시: 창문 가운데
        new Vector2(0.5f, 0.15f),       // 우주
    };

    // 이전 챕터 그림이 들어갈 창문 영역 (비율, 왼쪽 아래 기준). 크기가 0이면 창문 없음.
    [SerializeField] private Rect[] chapterPortals =
    {
        new Rect(0f, 0f, 0f, 0f),
        new Rect(0.52816f, 0.14713f, 0.02125f, 0.01256f), // 도시: 가운데 건물 창문
        new Rect(0f, 0f, 0f, 0f),
    };

    // 창문 안에 남는 이전 그림의 투명도
    [SerializeField, Range(0f, 1f)] private float portalAlpha = 0.45f;

    // 구름으로 넘어가는 챕터 (도시 → 우주). 구름이 양옆에서 덮었다가 갈라지며 다음 배경이 나온다.
    [SerializeField] private bool[] cloudTransitionInto = { false, false, true };

    // 구름 한 덩어리 그림. 비워두면 코드로 만든 임시 구름을 쓴다.
    [SerializeField] private Sprite cloudSprite;

    // 줌아웃 한 번의 배율 (StageManager의 StageShrinkRatio 0.5와 짝)
    private const float ZoomStep = 2f;

    enum PrevMode { None, Fade, Portal, Slide }

    private SpriteRenderer mainRenderer;  // 현재 챕터 배경
    private SpriteRenderer prevRenderer;  // 이전 챕터 배경
    private SpriteMask portalMask;        // 창문 모양으로 이전 배경을 잘라냄
    private GameManager gameManager;
    private Camera targetCamera;
    private Vector3 cameraStartPosition;

    private int mainChapter;
    private float mainZoom = 1f;

    private PrevMode prevMode = PrevMode.None;
    private int prevChapter;
    private float prevZoom = 1f;      // Fade 모드에서 이전 배경 배율
    private float portalBlend;        // Portal 모드: 0 = 원래 자리, 1 = 창문 안
    private float prevAlpha = 1f;

    private Vector3 pivot;
    private Coroutine running;

    // 배경이 화면보다 작아질 때 빈 곳을 채우는 그라데이션 (배경 맨 위/아래 색)
    private SpriteRenderer backdrop;
    private Texture2D backdropTexture;
    private int backdropChapter = -1;

    // 구름 전환
    private SpriteRenderer[] clouds = new SpriteRenderer[4];
    private float cloudAlpha;
    private float prevOffsetY;   // 이전 배경이 아래로 내려간 거리
    private float mainOffsetY;   // 다음 배경이 위에서 내려오는 거리

    void Awake()
    {
        mainRenderer = GetComponent<SpriteRenderer>();
        gameManager = FindFirstObjectByType<GameManager>();
        targetCamera = Camera.main;

        // 이전 챕터 배경: 현재 배경보다 한 칸 앞에 그린다.
        GameObject prevObject = new GameObject("Background_PrevChapter");
        prevRenderer = prevObject.AddComponent<SpriteRenderer>();
        prevRenderer.sharedMaterial = mainRenderer.sharedMaterial;
        prevRenderer.sortingLayerID = mainRenderer.sortingLayerID;
        prevRenderer.sortingOrder = mainRenderer.sortingOrder + 1;
        prevRenderer.enabled = false;

        // 창문 마스크: 1x1 크기 흰 사각형
        GameObject maskObject = new GameObject("Background_PortalMask");
        portalMask = maskObject.AddComponent<SpriteMask>();
        portalMask.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        portalMask.enabled = false;

        // 빈 곳 채우기용 그라데이션: 위아래 두 색을 부드럽게 늘린다.
        backdropTexture = new Texture2D(1, 2, TextureFormat.RGBA32, false);
        backdropTexture.filterMode = FilterMode.Bilinear;
        backdropTexture.wrapMode = TextureWrapMode.Clamp;
        backdrop = new GameObject("Background_Backdrop").AddComponent<SpriteRenderer>();
        backdrop.sprite = Sprite.Create(backdropTexture, new Rect(0, 0, 1, 2), new Vector2(0.5f, 0.5f), 1f);
        backdrop.sharedMaterial = mainRenderer.sharedMaterial;
        backdrop.sortingLayerID = mainRenderer.sortingLayerID;
        backdrop.sortingOrder = mainRenderer.sortingOrder - 1;

        // 구름: 배경보다 앞, 벽돌과 공보다 뒤
        Sprite cloud = cloudSprite != null ? cloudSprite : CreateCloudSprite();
        for (int i = 0; i < clouds.Length; i++)
        {
            clouds[i] = CreateCloud("Background_Cloud" + i, cloud, i % 2 == 1);
        }
    }

    SpriteRenderer CreateCloud(string objectName, Sprite sprite, bool flip)
    {
        SpriteRenderer sr = new GameObject(objectName).AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.flipX = flip;
        sr.sharedMaterial = mainRenderer.sharedMaterial;
        sr.sortingLayerID = mainRenderer.sortingLayerID;
        sr.sortingOrder = mainRenderer.sortingOrder + 2;
        sr.enabled = false;
        return sr;
    }

    void Start()
    {
        if (targetCamera != null)
        {
            cameraStartPosition = targetCamera.transform.position;
        }
        pivot = FindDefaultPivot();
        int stage = gameManager != null ? gameManager.currentStage : 1;
        mainChapter = GetChapter(stage);
        mainRenderer.sprite = GetSprite(mainChapter);
        mainZoom = GetZoom(stage);
    }

    void LateUpdate()
    {
        if (targetCamera == null || mainRenderer.sprite == null)
        {
            return;
        }

        Apply(mainRenderer, mainChapter, mainZoom, mainOffsetY);
        UpdateBackdrop();
        LayoutClouds();

        if (prevMode == PrevMode.Fade)
        {
            Apply(prevRenderer, prevChapter, prevZoom);
        }
        else if (prevMode == PrevMode.Slide)
        {
            Apply(prevRenderer, prevChapter, prevZoom, prevOffsetY);
        }
        else if (prevMode == PrevMode.Portal)
        {
            LayoutPortal();
        }

        if (prevMode != PrevMode.None)
        {
            prevRenderer.color = new Color(1f, 1f, 1f, prevAlpha);
        }
    }

    // StageManager가 스테이지 전환을 시작할 때 호출한다.
    public void PlayZoomOut(int nextStage, Vector3 shrinkPivot, float duration)
    {
        pivot = shrinkPivot;
        if (running != null)
        {
            StopCoroutine(running);
            FinishPrevious();
        }
        running = StartCoroutine(ZoomRoutine(nextStage, duration));
    }

    IEnumerator ZoomRoutine(int nextStage, float duration)
    {
        int nextChapter = GetChapter(nextStage);
        bool chapterChanged = nextChapter != mainChapter;
        float endZoom = GetZoom(nextStage);
        float startZoom = mainZoom;
        bool logZoom = false;

        // 이전 전환에서 창문 안에 남은 그림이 있으면 이번 전환 동안 완전히 사라지게 한다.
        bool fadeOutPortal = prevMode == PrevMode.Portal;
        float portalStartAlpha = prevAlpha;

        if (chapterChanged && UsesClouds(nextChapter))
        {
            ClearPrevious();
            yield return CloudRoutine(nextChapter, endZoom, duration);
            running = null;
            yield break;
        }

        if (chapterChanged)
        {
            prevChapter = mainChapter;
            prevRenderer.sprite = mainRenderer.sprite;
            prevRenderer.enabled = true;
            prevAlpha = 1f;
            fadeOutPortal = false;

            mainChapter = nextChapter;
            mainRenderer.sprite = GetSprite(nextChapter);

            if (HasPortal(nextChapter))
            {
                // 창문이 화면을 꽉 덮는 배율에서 시작해 챕터 시작 배율까지 줄어든다.
                prevMode = PrevMode.Portal;
                portalBlend = 0f;
                portalMask.enabled = true;
                prevRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                startZoom = Mathf.Max(ZoomToFillScreenWithPortal(nextChapter), endZoom * ZoomStep);
                logZoom = true;
            }
            else
            {
                // 이전 그림은 계속 줄어들며 흐려지고, 새 그림은 2배에서 시작해 뒤에서 함께 줄어든다.
                prevMode = PrevMode.Fade;
                prevZoom = mainZoom;
                prevRenderer.maskInteraction = SpriteMaskInteraction.None;
                startZoom = endZoom * ZoomStep;
            }
        }

        float prevStartZoom = prevZoom;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            SetProgress(t, startZoom, endZoom, logZoom, chapterChanged, prevStartZoom, fadeOutPortal, portalStartAlpha);
            yield return null;
        }
        SetProgress(1f, startZoom, endZoom, logZoom, chapterChanged, prevStartZoom, fadeOutPortal, portalStartAlpha);

        if (prevMode == PrevMode.Fade || (fadeOutPortal && prevMode == PrevMode.Portal))
        {
            ClearPrevious();
        }
        running = null;
    }

    void SetProgress(float t, float startZoom, float endZoom, bool logZoom, bool chapterChanged,
                     float prevStartZoom, bool fadeOutPortal, float portalStartAlpha)
    {
        // 챕터 안: StageManager와 같은 선형 보간이라 벽돌과 같은 속도로 줄어든다.
        // 창문으로 들어가는 큰 줌: 배율 차이가 커서 로그 보간으로 일정한 속도감을 준다.
        mainZoom = logZoom
            ? Mathf.Exp(Mathf.Lerp(Mathf.Log(startZoom), Mathf.Log(endZoom), t))
            : Mathf.Lerp(startZoom, endZoom, t);

        if (chapterChanged && prevMode == PrevMode.Fade)
        {
            prevZoom = Mathf.Lerp(prevStartZoom, prevStartZoom / ZoomStep, t);
            prevAlpha = 1f - t;
        }
        else if (chapterChanged && prevMode == PrevMode.Portal)
        {
            portalBlend = t;
            prevAlpha = Mathf.Lerp(1f, portalAlpha, t);
        }
        else if (fadeOutPortal)
        {
            prevAlpha = Mathf.Lerp(portalStartAlpha, 0f, t);
        }
    }

    // 중간에 끊긴 전환의 남은 상태를 정리한다.
    void FinishPrevious()
    {
        if (prevMode == PrevMode.Fade || prevMode == PrevMode.Slide)
        {
            mainOffsetY = 0f;
            prevOffsetY = 0f;
            foreach (SpriteRenderer c in clouds) c.enabled = false;
            ClearPrevious();
        }
        else if (prevMode == PrevMode.Portal)
        {
            portalBlend = 1f;
        }
    }

    void ClearPrevious()
    {
        prevMode = PrevMode.None;
        prevRenderer.enabled = false;
        prevRenderer.maskInteraction = SpriteMaskInteraction.None;
        portalMask.enabled = false;
    }

    // ---------- 구름 전환 (도시 → 우주) ----------
    // 이전 배경(도시)이 구름과 함께 아래로 쓸려 내려가며 작아지고,
    // 그 뒤에서 다음 배경(우주)이 위에서 내려오며 자리를 잡는다.
    // 구름은 작아진 도시 그림의 네 모서리만 살짝 가려서 그림 테두리가 티 나지 않게 한다.
    IEnumerator CloudRoutine(int nextChapter, float endZoom, float duration)
    {
        // 이전 배경을 앞쪽 렌더러로 옮긴다.
        prevChapter = mainChapter;
        prevRenderer.sprite = mainRenderer.sprite;
        prevRenderer.maskInteraction = SpriteMaskInteraction.None;
        prevRenderer.enabled = true;
        prevMode = PrevMode.Slide;
        prevZoom = mainZoom;
        prevAlpha = 1f;

        // 다음 배경은 뒤에서 확대된 채 위쪽에 있다가 내려온다.
        mainChapter = nextChapter;
        mainRenderer.sprite = GetSprite(nextChapter);

        float prevStartZoom = mainZoom;
        float prevEndZoom = mainZoom * 0.3f;
        float newStartZoom = endZoom * 1.6f;
        float screenHeight = targetCamera.orthographicSize * 2f;

        foreach (SpriteRenderer c in clouds) c.enabled = true;

        float time = 0f;
        while (true)
        {
            time += Time.deltaTime;
            float t = duration > 0f ? Mathf.Clamp01(time / duration) : 1f;
            float easeIn = t * t;                        // 점점 빨라지며 내려감
            float easeOut = 1f - (1f - t) * (1f - t);    // 빠르게 왔다가 천천히 자리 잡음

            prevZoom = Mathf.Exp(Mathf.Lerp(Mathf.Log(prevStartZoom), Mathf.Log(prevEndZoom), easeOut));
            prevOffsetY = -screenHeight * 0.9f * easeIn;
            prevAlpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1f, t));
            cloudAlpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.2f, t)) * prevAlpha;

            mainZoom = Mathf.Lerp(newStartZoom, endZoom, easeOut);
            mainOffsetY = screenHeight * 0.5f * (1f - easeOut);

            if (t >= 1f) break;
            yield return null;
        }

        mainZoom = endZoom;
        mainOffsetY = 0f;
        prevOffsetY = 0f;
        cloudAlpha = 0f;
        foreach (SpriteRenderer c in clouds) c.enabled = false;
        ClearPrevious();
    }

    bool UsesClouds(int chapter)
    {
        return cloudTransitionInto != null && chapter < cloudTransitionInto.Length && cloudTransitionInto[chapter];
    }

    // 작아지는 이전 배경의 네 모서리에 구름을 붙인다.
    void LayoutClouds()
    {
        if (!clouds[0].enabled) return;

        GetLayout(prevChapter, prevZoom, out _, out Vector2 bottomLeft, out Vector2 worldSize);
        bottomLeft.y += prevOffsetY;
        Vector2 cloudSize = clouds[0].sprite.bounds.size;
        float width = worldSize.x * 0.45f;
        float scale = width / cloudSize.x;
        float z = transform.position.z;

        Vector2[] corners =
        {
            new Vector2(bottomLeft.x, bottomLeft.y + worldSize.y),                 // 왼쪽 위
            new Vector2(bottomLeft.x + worldSize.x, bottomLeft.y + worldSize.y),   // 오른쪽 위
            new Vector2(bottomLeft.x, bottomLeft.y),                               // 왼쪽 아래
            new Vector2(bottomLeft.x + worldSize.x, bottomLeft.y),                 // 오른쪽 아래
        };

        for (int i = 0; i < clouds.Length; i++)
        {
            clouds[i].transform.localScale = new Vector3(scale, scale, 1f);
            clouds[i].transform.position = new Vector3(corners[i].x, corners[i].y, z);
            clouds[i].color = new Color(1f, 1f, 1f, cloudAlpha);
        }
    }

    // 몽글몽글한 도트 구름 한 덩어리 (가로로 긴 모양)
    static Sprite CreateCloudSprite()
    {
        const int w = 40, h = 22;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        // 원 여러 개를 겹쳐 구름 모양을 만든다. (x, y, 반지름)
        Vector3[] puffs =
        {
            new Vector3(10, 8, 7), new Vector3(19, 12, 9), new Vector3(29, 9, 8), new Vector3(20, 6, 6), new Vector3(33, 6, 5),
        };
        Color white = Color.white;
        Color shade = new Color(0.86f, 0.9f, 0.97f, 1f);
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                bool inside = false;
                foreach (Vector3 p in puffs)
                {
                    float dx = x - p.x, dy = y - p.y;
                    if (dx * dx + dy * dy <= p.z * p.z) inside = true;
                }
                // 아래쪽 몇 줄은 살짝 푸른 그림자
                tex.SetPixel(x, y, inside ? (y < 4 ? shade : white) : Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
    }

    // 현재 챕터 배경의 맨 위/아래 색으로 그라데이션을 만들고 화면에 깐다.
    void UpdateBackdrop()
    {
        Rect screen = ScreenRect();
        backdrop.transform.position = new Vector3(screen.center.x, screen.center.y, transform.position.z + 1f);
        backdrop.transform.localScale = new Vector3(screen.width * 1.1f, screen.height * 1.1f / 2f, 1f);

        if (backdropChapter == mainChapter) return;
        backdropChapter = mainChapter;

        Sprite sprite = mainRenderer.sprite;
        Color top = targetCamera.backgroundColor, bottom = targetCamera.backgroundColor;
        if (sprite != null && sprite.texture != null && sprite.texture.isReadable)
        {
            Rect r = sprite.textureRect;
            top = AverageRow(sprite.texture, r, (int)r.yMax - 2);
            bottom = AverageRow(sprite.texture, r, (int)r.yMin + 1);
        }
        top.a = bottom.a = 1f;
        backdropTexture.SetPixel(0, 0, bottom);
        backdropTexture.SetPixel(0, 1, top);
        backdropTexture.Apply();
    }

    static Color AverageRow(Texture2D tex, Rect r, int y)
    {
        Color sum = Color.clear;
        int n = 0;
        for (int x = (int)r.xMin; x < (int)r.xMax; x += 8)
        {
            sum += tex.GetPixel(x, y);
            n++;
        }
        return n > 0 ? sum / n : Color.gray;
    }

    // ---------- 배율과 챕터 ----------

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
            : GameManager.FinalStage;
        int stepsLeft = Mathf.Max(0, chapterEnd - stage);
        return Mathf.Pow(ZoomStep, stepsLeft);
    }

    Sprite GetSprite(int chapter)
    {
        if (chapterSprites == null || chapter >= chapterSprites.Length)
        {
            return mainRenderer.sprite;
        }
        return chapterSprites[chapter];
    }

    Vector2 GetAnchor(int chapter)
    {
        Vector2 a = chapterAnchors != null && chapter < chapterAnchors.Length
            ? chapterAnchors[chapter]
            : new Vector2(0.5f, 0.5f);
        return new Vector2(Mathf.Clamp(a.x, 0.01f, 0.99f), Mathf.Clamp(a.y, 0.01f, 0.99f));
    }

    bool HasPortal(int chapter)
    {
        return chapterPortals != null && chapter < chapterPortals.Length
            && chapterPortals[chapter].width > 0f && chapterPortals[chapter].height > 0f;
    }

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

    // ---------- 화면 배치 계산 ----------

    // 카메라 흔들림을 따라가도록 기준점에 카메라 이동량을 더한다.
    Vector2 ScreenPivot()
    {
        Vector3 shake = targetCamera.transform.position - cameraStartPosition;
        return new Vector2(pivot.x + shake.x, pivot.y + shake.y);
    }

    Rect ScreenRect()
    {
        float h = targetCamera.orthographicSize * 2f;
        float w = h * targetCamera.aspect;
        Vector3 c = targetCamera.transform.position;
        return new Rect(c.x - w / 2f, c.y - h / 2f, w, h);
    }

    // 배율 1에서 Anchor를 기준점에 두고 화면을 빈틈없이 덮는 최소 크기
    float BaseScale(Sprite sprite, Vector2 anchor)
    {
        Vector2 size = sprite.bounds.size;
        Vector2 p = ScreenPivot();
        Rect screen = ScreenRect();
        float s = 0f;
        s = Mathf.Max(s, (p.x - screen.xMin) / (anchor.x * size.x));
        s = Mathf.Max(s, (screen.xMax - p.x) / ((1f - anchor.x) * size.x));
        s = Mathf.Max(s, (p.y - screen.yMin) / (anchor.y * size.y));
        s = Mathf.Max(s, (screen.yMax - p.y) / ((1f - anchor.y) * size.y));
        return s;
    }

    // 챕터 배경의 실제 크기(scale)와 왼쪽 아래 좌표
    void GetLayout(int chapter, float zoom, out float scale, out Vector2 bottomLeft, out Vector2 worldSize)
    {
        Sprite sprite = GetSprite(chapter);
        Vector2 anchor = GetAnchor(chapter);
        scale = BaseScale(sprite, anchor) * zoom;
        worldSize = (Vector2)sprite.bounds.size * scale;
        bottomLeft = ScreenPivot() - new Vector2(anchor.x * worldSize.x, anchor.y * worldSize.y);
    }

    void Apply(SpriteRenderer target, int chapter, float zoom, float offsetY = 0f)
    {
        GetLayout(chapter, zoom, out float scale, out Vector2 bottomLeft, out Vector2 worldSize);
        SetTransform(target.transform, bottomLeft + worldSize / 2f + new Vector2(0f, offsetY), scale);
    }

    void SetTransform(Transform target, Vector2 center, float scale)
    {
        target.localScale = new Vector3(scale, scale, 1f);
        target.position = new Vector3(center.x, center.y, transform.position.z);
    }

    // 현재 배경의 창문이 화면에서 차지하는 영역
    Rect PortalWorldRect(int chapter, float zoom)
    {
        GetLayout(chapter, zoom, out _, out Vector2 bottomLeft, out Vector2 worldSize);
        Rect r = chapterPortals[chapter];
        return new Rect(
            bottomLeft.x + r.x * worldSize.x,
            bottomLeft.y + r.y * worldSize.y,
            r.width * worldSize.x,
            r.height * worldSize.y);
    }

    // 창문이 화면 전체를 덮으려면 필요한 배율
    float ZoomToFillScreenWithPortal(int chapter)
    {
        Rect unit = PortalWorldRect(chapter, 1f);
        Vector2 p = ScreenPivot();
        Rect screen = ScreenRect();
        // 배율 z에서 창문의 각 변은 기준점으로부터 z배 멀어진다.
        float z = 1f;
        if (unit.xMin < p.x) z = Mathf.Max(z, (p.x - screen.xMin) / (p.x - unit.xMin));
        if (unit.xMax > p.x) z = Mathf.Max(z, (screen.xMax - p.x) / (unit.xMax - p.x));
        if (unit.yMin < p.y) z = Mathf.Max(z, (p.y - screen.yMin) / (p.y - unit.yMin));
        if (unit.yMax > p.y) z = Mathf.Max(z, (screen.yMax - p.y) / (unit.yMax - p.y));
        return z;
    }

    // 이전 배경을 원래 자리(portalBlend 0)에서 창문 안(1)으로 옮기고 창문 모양으로 잘라낸다.
    void LayoutPortal()
    {
        Rect window = PortalWorldRect(mainChapter, mainZoom);

        // 마스크 = 창문
        portalMask.transform.position = new Vector3(window.center.x, window.center.y, transform.position.z);
        portalMask.transform.localScale = new Vector3(window.width, window.height, 1f);

        // 원래 자리: 이전 챕터의 배율 1 배치
        GetLayout(prevChapter, 1f, out float ownScale, out Vector2 ownBottomLeft, out Vector2 ownSize);
        Vector2 ownCenter = ownBottomLeft + ownSize / 2f;

        // 창문 안: 창문을 빈틈없이 덮는 크기
        Vector2 spriteSize = prevRenderer.sprite.bounds.size;
        float windowScale = Mathf.Max(window.width / spriteSize.x, window.height / spriteSize.y);

        float scale = Mathf.Exp(Mathf.Lerp(Mathf.Log(ownScale), Mathf.Log(windowScale), portalBlend));
        Vector2 center = Vector2.Lerp(ownCenter, window.center, portalBlend);

        // 화면에 보이는 창문 부분에 빈틈이 생기지 않게 크기와 위치를 보정한다.
        Rect screen = ScreenRect();
        float xMin = Mathf.Max(window.xMin, screen.xMin), xMax = Mathf.Min(window.xMax, screen.xMax);
        float yMin = Mathf.Max(window.yMin, screen.yMin), yMax = Mathf.Min(window.yMax, screen.yMax);
        if (xMax > xMin && yMax > yMin)
        {
            scale = Mathf.Max(scale, (xMax - xMin) / spriteSize.x, (yMax - yMin) / spriteSize.y);
            float halfW = spriteSize.x * scale / 2f, halfH = spriteSize.y * scale / 2f;
            center.x = Mathf.Clamp(center.x, xMax - halfW, xMin + halfW);
            center.y = Mathf.Clamp(center.y, yMax - halfH, yMin + halfH);
        }
        SetTransform(prevRenderer.transform, center, scale);
    }
}
