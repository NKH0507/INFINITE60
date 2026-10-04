using UnityEngine;

public class stagemanager : MonoBehaviour
{
    // 현재 생성되어 있는 스테이지
    GameObject currentStage;


    // StageData에 저장된 스테이지 프리팹을 생성한다.
    public void CreateStage(stage stageData)
    {

        // 기존 스테이지가 남아있으면 삭제
        if (currentStage != null)
        {
            Destroy(currentStage);
        }

        // StageData에 저장된 프리팹을 생성
        currentStage = Instantiate(stageData.stagePrefab,Vector3.zero,Quaternion.identity);
    }

    // 현재 스테이지의 벽돌 개수를 반환한다.
    public int GetBrickCount()
    {

        // 현재 스테이지가 없으면 0 반환
        if (currentStage == null)
        {
            return 0;
        }

        // Stage_1 밑에 있는 벽돌 개수 반환
        return currentStage.transform.childCount;
    }

    // 현재 스테이지를 삭제한다.
    public void ClearStage()
    {

        // 현재 생성된 스테이지가 있는 경우
        if (currentStage != null)
        {
            Destroy(currentStage);

            // 삭제했으므로 null로 설정
            currentStage = null;
        }

    }

}