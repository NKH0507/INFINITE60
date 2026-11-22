using UnityEngine;

// 챕터별 배경을 보여준다.
// 카메라를 따라다니며 화면을 빈틈없이 덮도록 크기를 맞추고,
// GameManager의 현재 스테이지가 바뀌면 해당 챕터의 배경으로 바꾼다.
[RequireComponent(typeof(SpriteRenderer))]
public class ChapterBackground : MonoBehaviour
{
    // 0: 1챕터(방), 1: 2챕터(도시), 2: 3챕터(우주)
    [SerializeField] private Sprite[] chapterSprites;

    // 각 챕터가 시작되는 스테이지 번호 (1~3, 4~6, 7~8)
    [SerializeField] private int[] chapterStartStages = { 1, 4, 7 };

    private SpriteRenderer spriteRenderer;
    private GameManager gameManager;
    private Camera targetCamera;
    private int shownChapter = -1;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        gameManager = FindFirstObjectByType<GameManager>();
        targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        int stage = gameManager != null ? gameManager.currentStage : 1;
        int chapter = GetChapter(stage);

        if (chapter != shownChapter)
        {
            shownChapter = chapter;
            if (chapterSprites != null && chapter < chapterSprites.Length)
            {
                spriteRenderer.sprite = chapterSprites[chapter];
            }
        }

        FitToCamera();
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

    // 화면 전체를 덮도록 배경 크기와 위치를 맞춘다. (남는 부분은 잘림)
    void FitToCamera()
    {
        if (targetCamera == null || spriteRenderer.sprite == null)
        {
            return;
        }

        float screenHeight = targetCamera.orthographicSize * 2f;
        float screenWidth = screenHeight * targetCamera.aspect;
        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        float scale = Mathf.Max(screenWidth / spriteSize.x, screenHeight / spriteSize.y);
        transform.localScale = new Vector3(scale, scale, 1f);

        Vector3 camPos = targetCamera.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, transform.position.z);
    }
}
