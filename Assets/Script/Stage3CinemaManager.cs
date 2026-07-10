using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Stage3CinematicManager : MonoBehaviour
{
    public static Stage3CinematicManager Instance { get; private set; }

    [Header("연동할 컴포넌트")]
    [Tooltip("메인 카메라를 연결하세요.")]
    [SerializeField] private Camera mainCamera;
    [Tooltip("인게임에서 사용하는 UI 부모 오브젝트를 연결하세요. (시네마 바와 페이드 이미지는 이 오브젝트의 자식이 아니어야 합니다!)")]
    [SerializeField] private GameObject inGameUiCanvas;

    [Header("시네마틱 연출 UI (평소에 꺼두셔도 작동합니다)")]
    [Tooltip("화면 위쪽을 가릴 검은색 레터박스 Image입니다.")]
    [SerializeField] private RectTransform topCinemaBar;
    [Tooltip("화면 아래쪽을 가릴 검은색 레터박스 Image입니다.")]
    [SerializeField] private RectTransform bottomCinemaBar;
    [Tooltip("화면 전체 페이드용 암전 Image입니다.")]
    [SerializeField] private Image fadeOverlayImage;

    // 고정 연출 수치 설정
    private Vector3 forcedDummyPosition = new Vector3(148f, 122f, 0f);
    private float cameraMoveDistance = 15f;   
    
    // 타임라인 시간 설정
    private float cameraMoveDuration = 5.0f;  // 5초 동안 카메라 이동
    private float cameraIdleDuration = 1.0f;  // 1초 동안 정지 대기
    private float fadeOutDuration = 1.0f;     // 1초 동안 암전
    private float fadeInDuration = 0.6f;      // 0.6초 동안 밝아짐

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 💡 [수정] 게임이 시작되면 에디터에서 켜져있든 꺼져있든 상태를 완전히 투명하게 리셋합니다.
        ResetCinematicUI();
    }

    private void ResetCinematicUI()
    {
        // 평소 개발 편의를 위해 꺼둔 오브젝트들을 메모리 상에서 안전하게 알파만 0으로 밀어둡니다.
        if (fadeOverlayImage != null) 
        { 
            fadeOverlayImage.gameObject.SetActive(false); 
            SetAlpha(fadeOverlayImage, 0f);
        }
        if (topCinemaBar != null) topCinemaBar.gameObject.SetActive(false);
        if (bottomCinemaBar != null) bottomCinemaBar.gameObject.SetActive(false);
    }

    public void PlayStage3Cinematic(PlayerMovement player, Vector3 dummyMapPos, Vector3 realStage3Pos)
    {
        StartCoroutine(CinematicRoutine(player, forcedDummyPosition, realStage3Pos));
    }

    private IEnumerator CinematicRoutine(PlayerMovement player, Vector3 dummyMapPos, Vector3 realStage3Pos)
    {
        if (player == null || mainCamera == null) yield break;

        // -----------------------------------------------------------------
        // STEP 1. 연출 초기화 (꺼져있던 시네마 바와 페이드를 코드에서 켭니다)
        // -----------------------------------------------------------------
        if (inGameUiCanvas != null) inGameUiCanvas.SetActive(false);

        // 💡 [수정] 발판을 밟는 순간, 하이어라키에서 꺼져있던 오브젝트들을 확실하게 SetActive(true) 시킵니다.
        if (topCinemaBar != null) topCinemaBar.gameObject.SetActive(true);
        if (bottomCinemaBar != null) bottomCinemaBar.gameObject.SetActive(true);

        player.TeleportTo(dummyMapPos);

        Vector3 camStartPos = new Vector3(dummyMapPos.x, dummyMapPos.y, -10f);
        mainCamera.transform.position = camStartPos;

        yield return new WaitForSeconds(0.2f); 

        // -----------------------------------------------------------------
        // STEP 2. [5초 동안] 카메라 Y좌표가 +15만큼 스르륵 상승
        // -----------------------------------------------------------------
        Vector3 camTargetPos = camStartPos + (Vector3.up * cameraMoveDistance);
        float elapsed = 0f;

        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cameraMoveDuration;
            t = t * t * (3f - 2f * t); 

            mainCamera.transform.position = Vector3.Lerp(camStartPos, camTargetPos, t);
            yield return null;
        }
        mainCamera.transform.position = camTargetPos; 

        // -----------------------------------------------------------------
        // STEP 3. [1초 동안] 카메라 최고 지점 정지
        // -----------------------------------------------------------------
        yield return new WaitForSeconds(cameraIdleDuration);

        // -----------------------------------------------------------------
        // STEP 4. [1초 동안] 서서히 화면 암전 (Fade Out)
        // -----------------------------------------------------------------
        if (fadeOverlayImage != null)
        {
            // 💡 꺼져있던 암전용 이미지를 여기서 켜서 연산합니다.
            fadeOverlayImage.gameObject.SetActive(true); 
            elapsed = 0f;
            while (elapsed < fadeOutDuration) 
            {
                elapsed += Time.deltaTime;
                SetAlpha(fadeOverlayImage, Mathf.Clamp01(elapsed / fadeOutDuration));
                yield return null;
            }
            SetAlpha(fadeOverlayImage, 1f);
        }

        yield return new WaitForSeconds(0.3f); 

        // -----------------------------------------------------------------
        // STEP 5. 진짜 3스테이지 위치(28, 123) 복귀 및 시네마 바 종료
        // -----------------------------------------------------------------
        player.TeleportTo(realStage3Pos);
        mainCamera.transform.position = new Vector3(realStage3Pos.x, realStage3Pos.y, -10f);
    

        // 연출용 시네마 바는 볼일 끝났으니 다시 꺼줍니다.
        if (topCinemaBar != null) topCinemaBar.gameObject.SetActive(false);
        if (bottomCinemaBar != null) bottomCinemaBar.gameObject.SetActive(false);
        if (inGameUiCanvas != null) inGameUiCanvas.SetActive(true);
        
        WitchTrilogyBoss.Instance.ShowBossUI();

        // -----------------------------------------------------------------
        // STEP 6. 화면 페이드 인 및 암전 오브젝트 완전 종료
        // -----------------------------------------------------------------
        if (fadeOverlayImage != null)
        {
            elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                SetAlpha(fadeOverlayImage, 1f - Mathf.Clamp01(elapsed / fadeInDuration));
                yield return null;
            }
            SetAlpha(fadeOverlayImage, 0f);
            
            // 💡 다음 개발이나 재생을 위해 다시 완전 비활성화(Set Player View Clear) 처리합니다.
            fadeOverlayImage.gameObject.SetActive(false); 
        }

        Debug.Log("[Cinematic] 컷신 완벽 종료 및 UI 원상 복구 완료.");
    }

    private void SetAlpha(Image img, float alpha)
    {
        if (img == null) return;
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }
}