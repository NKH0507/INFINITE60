using System.Collections.Generic;
using UnityEngine;

// 벽돌이 깨질 때 삼각형 조각이 흩어졌다가, 도로록 하나씩 공으로 빨려 들어가는 연출.
// 조각이 공에 닿을 때마다 공이 반짝이며 조금씩 커진다. (Ball.OnFragmentAbsorbed)
//
// 씬에 따로 둘 필요 없이 처음 쓰일 때 스스로 만들어진다.
// 조각 오브젝트는 재사용(풀링)해서 휴대폰에서도 부담이 적다.
public class AbsorbFx : MonoBehaviour
{
    [Header("조각")]
    [SerializeField] int fragmentsPerBrick = 5;
    [SerializeField] int maxActiveFragments = 120;   // 관통으로 한꺼번에 깨질 때 상한
    [SerializeField] float fragmentSize = 0.22f;      // 벽돌 가로 길이 대비 조각 크기

    [Header("시간 (초)")]
    [SerializeField] float scatterTime = 0.2f;        // 흩어지는 시간
    [SerializeField] float holdTime = 0.1f;           // 잠깐 멈칫
    [SerializeField] float stagger = 0.05f;           // 조각마다 출발 간격 (도로록)
    [SerializeField] float maxFlyTime = 0.6f;         // 이 시간 안에 못 닿으면 바로 흡수

    [Header("속도")]
    [SerializeField] float scatterSpeed = 3f;         // 벽돌 가로 길이 기준 배수
    [SerializeField] float homingStartSpeed = 4f;
    [SerializeField] float homingAcceleration = 60f;

    static AbsorbFx instance;
    static Sprite triangleSprite;
    static readonly Dictionary<Sprite, Color[]> paletteCache = new Dictionary<Sprite, Color[]>();

    class Fragment
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector3 velocity;
        public float spin;
        public float age;
        public float homeAt;       // 공을 향해 출발하는 시각
        public float speed;
        public float share;        // 공에 닿을 때 보이는 크기에 더할 공격력
        public float size;         // 처음 크기
        public bool active;
    }

    readonly List<Fragment> fragments = new List<Fragment>();
    Ball ball;
    int activeCount;

    // 벽돌이 깨질 때 호출한다. absorbAmount는 공에 더해진 공격력.
    public static void Spawn(Brick brick, SpriteRenderer brickRenderer, Ball targetBall, float absorbAmount)
    {
        if (targetBall == null) return;
        if (instance == null)
        {
            instance = new GameObject("AbsorbFx").AddComponent<AbsorbFx>();
        }
        instance.SpawnInternal(brick, brickRenderer, targetBall, absorbAmount);
    }

    void SpawnInternal(Brick brick, SpriteRenderer brickRenderer, Ball targetBall, float absorbAmount)
    {
        ball = targetBall;

        // 전환 중이거나 조각이 너무 많으면 연출 없이 바로 반영한다.
        int count = Mathf.Max(1, fragmentsPerBrick);
        if (ball.isTransitioning || activeCount + count > maxActiveFragments || brickRenderer == null)
        {
            ball.OnFragmentAbsorbed(absorbAmount, false);
            return;
        }

        ball.NotifyAbsorbQueued();

        Bounds bounds = brickRenderer.bounds;
        float width = Mathf.Max(bounds.size.x, 0.01f);
        Color[] palette = GetPalette(brickRenderer);
        float share = absorbAmount / count;

        for (int i = 0; i < count; i++)
        {
            Fragment f = GetFragment();
            f.active = true;
            activeCount++;

            // 벽돌 안의 아무 지점에서 바깥으로 흩어진다.
            Vector3 start = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                0f);
            Vector2 dir = Random.insideUnitCircle.normalized;
            f.transform.position = start;
            f.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            float size = width * fragmentSize * Random.Range(0.7f, 1.2f);
            f.transform.localScale = new Vector3(size, size, 1f);
            f.size = size;

            f.velocity = (Vector3)(dir * width * scatterSpeed * Random.Range(0.6f, 1.2f));
            f.spin = Random.Range(-720f, 720f);
            f.age = 0f;
            f.homeAt = scatterTime + holdTime + stagger * i;
            f.speed = homingStartSpeed;
            f.share = share;

            f.renderer.color = palette[Random.Range(0, palette.Length)];
            f.renderer.sortingLayerID = brickRenderer.sortingLayerID;
            f.renderer.sortingOrder = brickRenderer.sortingOrder + 5;
            f.renderer.enabled = true;
        }
    }

    void Update()
    {
        if (activeCount == 0) return;

        float dt = Time.deltaTime;
        bool flushAll = ball == null || ball.isTransitioning;

        for (int i = 0; i < fragments.Count; i++)
        {
            Fragment f = fragments[i];
            if (!f.active) continue;

            if (flushAll)
            {
                Absorb(f, false);
                continue;
            }

            f.age += dt;
            f.transform.Rotate(0f, 0f, f.spin * dt);

            if (f.age < f.homeAt)
            {
                // 흩어지다가 점점 느려져서 멈칫한다.
                f.transform.position += f.velocity * dt;
                f.velocity = Vector3.Lerp(f.velocity, Vector3.zero, 12f * dt);
                continue;
            }

            // 공의 현재 위치를 쫓아가며 점점 빨라진다.
            Vector3 target = ball.transform.position;
            Vector3 pos = f.transform.position;
            Vector3 toBall = target - pos;
            toBall.z = 0f;
            f.speed += homingAcceleration * dt;
            float step = f.speed * dt;
            float ballRadius = ball.transform.lossyScale.x * 0.5f;

            if (toBall.magnitude <= step + ballRadius * 0.5f || f.age - f.homeAt > maxFlyTime)
            {
                Absorb(f, true);
                continue;
            }

            f.transform.position = pos + toBall.normalized * step;

            // 공에 가까워질수록 살짝 작아진다.
            float shrink = Mathf.Clamp01(toBall.magnitude / (ballRadius * 4f + 0.01f));
            float s = Mathf.Lerp(0.5f, 1f, shrink) * f.size;
            f.transform.localScale = new Vector3(s, s, 1f);
        }
    }

    void Absorb(Fragment f, bool withEffect)
    {
        f.active = false;
        f.renderer.enabled = false;
        activeCount--;
        if (ball != null)
        {
            ball.OnFragmentAbsorbed(f.share, withEffect);
        }
    }

    Fragment GetFragment()
    {
        for (int i = 0; i < fragments.Count; i++)
        {
            if (!fragments[i].active) return fragments[i];
        }

        GameObject go = new GameObject("Fragment");
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetTriangleSprite();
        Fragment f = new Fragment { transform = go.transform, renderer = sr };
        fragments.Add(f);
        return f;
    }

    // 8x8 도트 삼각형을 코드로 만든다. (아트 파일 없이)
    static Sprite GetTriangleSprite()
    {
        if (triangleSprite != null) return triangleSprite;

        const int n = 8;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < n; y++)
        {
            // 아래가 넓고 위로 갈수록 좁아지는 삼각형
            int half = (n - y) / 2;
            for (int x = 0; x < n; x++)
            {
                bool inside = Mathf.Abs(x - (n - 1) / 2f) <= half;
                tex.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }
        tex.Apply();
        triangleSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.4f), n);
        return triangleSprite;
    }

    // 벽돌 그림에서 색을 몇 개 뽑아 조각 색으로 쓴다. (이미지 Read/Write가 켜져 있어야 함)
    static Color[] GetPalette(SpriteRenderer sr)
    {
        Sprite sprite = sr.sprite;
        if (sprite == null) return new[] { sr.color };
        if (paletteCache.TryGetValue(sprite, out Color[] cached)) return cached;

        List<Color> colors = new List<Color>();
        Texture2D tex = sprite.texture;
        if (tex != null && tex.isReadable)
        {
            Rect r = sprite.textureRect;
            for (int i = 0; i < 40 && colors.Count < 12; i++)
            {
                int x = (int)Random.Range(r.xMin + r.width * 0.1f, r.xMax - r.width * 0.1f);
                int y = (int)Random.Range(r.yMin + r.height * 0.1f, r.yMax - r.height * 0.1f);
                Color c = tex.GetPixel(x, y);
                if (c.a > 0.5f) colors.Add(c * sr.color);
            }
        }
        if (colors.Count == 0) colors.Add(sr.color);

        Color[] palette = colors.ToArray();
        paletteCache[sprite] = palette;
        return palette;
    }
}
