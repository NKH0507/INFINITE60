using UnityEngine;

// Unity에서 Stage Data 파일을 직접 만들 수 있게 해준다.
[CreateAssetMenu(fileName = "StageData", menuName = "Game/Stage Data")]
public class stage : ScriptableObject
{
    // 스테이지 번호
    public int stageNumber;

    // 이 스테이지에서 사용할 스테이지 프리팹
    public GameObject stagePrefab;
}