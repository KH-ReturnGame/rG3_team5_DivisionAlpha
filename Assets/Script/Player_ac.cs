using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.InputSystem;

public class Player_ac : MonoBehaviour
{
    private bool _isMoving;
    private Vector3 _origPos, _targetPos;
    private float _timeToMove = 0.13f;

    [Header("인간 방향별 스프라이트 등록")]
    [SerializeField] private Sprite spriteUp;    
    [SerializeField] private Sprite spriteDown;  
    [SerializeField] private Sprite spriteLeft;  
    [SerializeField] private Sprite spriteRight; 

    private SpriteRenderer _spriteRenderer; 

    [Header("1스킬: 즉시 관통 화살 발사 (J키)")]
    [SerializeField] private GameObject arrowEffectObject; 
    [SerializeField] private Image arrowBorderImage;   // 1스킬 빨간 테두리 Image
    [SerializeField] private Image arrowCoolDownImage;   // 1스킬 안쪽 흰색 원형 게이지 Image
    
    [SerializeField] private float arrowDuration = 0.2f; // 이펙트 노출 시간 0.2초
    [SerializeField] private float arrowCooldown = 1.0f; // 쿨타임 1.0초
    private float _arrowCooldownTimer = 0f;
    [SerializeField] private int arrowRange = 5;          // 화살 사거리 (타일 기준)

    [Header("2스킬: 백스텝 (K키)")]
    [SerializeField] private GameObject backstepEffectObject; 
    [SerializeField] private Image backstepBorderImage;  // 2스킬 빨간 테두리 Image
    [SerializeField] private Image backstepCoolDownImage; // 2스킬 안쪽 흰색 원형 게이지 Image
    [SerializeField] private float backstepDuration = 0.3f; // 이동 시간
    [SerializeField] private float backstepCooldown = 5.0f; // 쿨타임 5.0초
    private float _backstepCooldownTimer = 0f;

    private Vector3 _lookDirection = Vector3.down; 
    private Coroutine _arrowEffectCoroutine;
    private Coroutine _backstepEffectCoroutine;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (arrowEffectObject != null) arrowEffectObject.SetActive(false);
        if (backstepEffectObject != null) backstepEffectObject.SetActive(false);

        // [초기화] 게임 시작할 때 UI들의 기본 상태를 강제로 세팅
        ResetUIState(arrowBorderImage, arrowCoolDownImage);
        ResetUIState(backstepBorderImage, backstepCoolDownImage);

        UpdateGridSortingOrder();
    }

    void Update()
    {
        // 쿨타임 타이머가 작동 중일 때만 UI를 갱신
        if (_arrowCooldownTimer > 0)
        {
            UpdateCooldownUI(ref _arrowCooldownTimer, arrowCooldown, arrowBorderImage, arrowCoolDownImage);
        }
        if (_backstepCooldownTimer > 0)
        {
            UpdateCooldownUI(ref _backstepCooldownTimer, backstepCooldown, backstepBorderImage, backstepCoolDownImage);
        }

        // 1. 이동 처리
        if (Keyboard.current.wKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.up;
            ChangePlayerSprite(Vector3.up);
            StartCoroutine(MovePlayer(Vector3.up));
        }
        else if (Keyboard.current.aKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.left;
            ChangePlayerSprite(Vector3.left);
            StartCoroutine(MovePlayer(Vector3.left));
        }
        else if (Keyboard.current.sKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.down;
            ChangePlayerSprite(Vector3.down);
            StartCoroutine(MovePlayer(Vector3.down));
        }
        else if (Keyboard.current.dKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.right;
            ChangePlayerSprite(Vector3.right);
            StartCoroutine(MovePlayer(Vector3.right));
        }

        // 2. 1스킬 즉시 관통 화살 발사 (J키)
        if (Keyboard.current.jKey.wasPressedThisFrame && _arrowCooldownTimer <= 0)
        {
            TryArrowAttack();
        }

        // 3. 2스킬 백스텝 (K키)
        if (Keyboard.current.kKey.wasPressedThisFrame && _backstepCooldownTimer <= 0)
        {
            TryBackstep();
        }
    }

    // UI 상태를 안전하게 제어하는 핵심 함수
    private void UpdateCooldownUI(ref float timer, float maxCooldown, Image borderImg, Image coolDownImg)
    {
        timer -= Time.deltaTime;

        if (timer > 0)
        {
            float fillRatio = (maxCooldown - timer) / maxCooldown;

            // 빨간 테두리: 시계방향 차오름 연산
            if (borderImg != null && borderImg.type == Image.Type.Filled)
            {
                borderImg.fillAmount = fillRatio;
            }

            // 안쪽 흰색 원: 투명도 25%로 낮춤
            if (coolDownImg != null)
            {
                Color cColor = coolDownImg.color;
                cColor.a = 0.25f; 
                coolDownImg.color = cColor;
                coolDownImg.fillAmount = 1f; 
            }
        }
        else
        {
            // 쿨타임이 딱 끝난 순간 복구
            timer = 0f;
            ResetUIState(borderImg, coolDownImg);
        }
    }

    // 평소 대기 상태로 UI를 되돌리는 분리 함수
    private void ResetUIState(Image borderImg, Image coolDownImg)
    {
        if (borderImg != null)
        {
            borderImg.fillAmount = 1f; // 테두리 100% 장착
        }
        if (coolDownImg != null)
        {
            Color cColor = coolDownImg.color;
            cColor.a = 0.6f; // 안쪽 원 원래 선명도(60%) 복구
            coolDownImg.color = cColor;
            coolDownImg.fillAmount = 1f;
        }
    }

    private void ChangePlayerSprite(Vector3 direction)
    {
        if (_spriteRenderer == null) return;
        if (direction == Vector3.up && spriteUp != null) _spriteRenderer.sprite = spriteUp;
        else if (direction == Vector3.down && spriteDown != null) _spriteRenderer.sprite = spriteDown;
        else if (direction == Vector3.left && spriteLeft != null) _spriteRenderer.sprite = spriteLeft;
        else if (direction == Vector3.right && spriteRight != null) _spriteRenderer.sprite = spriteRight;
    }

    private IEnumerator MovePlayer(Vector3 direction)
    {
        _isMoving = true;
        float elapsedTime = 0;
        _origPos = transform.position;
        _targetPos = _origPos + direction;
        while (elapsedTime < _timeToMove)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(_origPos, _targetPos, elapsedTime / _timeToMove);
            yield return null;
        }
        transform.position = _targetPos;
        
        UpdateGridSortingOrder();
        _isMoving = false;
    }

    private void TryArrowAttack()
    {
        Debug.Log("인간 1스킬 즉시 관통 화살 발사 (J키)!");
        _arrowCooldownTimer = arrowCooldown; // 쿨타임 타이머 시동
        
        // 스킬을 누른 첫 프레임에 테두리를 0으로 만들고 쿨타임 UI 연출 돌입
        if (arrowBorderImage != null) arrowBorderImage.fillAmount = 0f;

        if (_arrowEffectCoroutine != null) StopCoroutine(_arrowEffectCoroutine);
        _arrowEffectCoroutine = StartCoroutine(ShowArrowEffectRoutine());

        // 사거리 내 일직선 타일 계산
        int myX = Mathf.RoundToInt(transform.position.x);
        int myY = Mathf.RoundToInt(transform.position.y);
        List<Vector2Int> targetTiles = GetFrontTiles(myX, myY, arrowRange);

        ZombieAIWithCounter[] zombies = Object.FindObjectsByType<ZombieAIWithCounter>(FindObjectsSortMode.None);
        foreach (ZombieAIWithCounter zombie in zombies)
        {
            int zombieX = Mathf.RoundToInt(zombie.transform.position.x);
            int zombieY = Mathf.RoundToInt(zombie.transform.position.y);

            foreach (Vector2Int tile in targetTiles)
            {
                if (zombieX == tile.x && zombieY == tile.y)
                {
                    Debug.Log($"좀비가 즉시 발사된 관통 화살에 맞아 데미지를 입음! 위치: {tile}");
                    zombie.OnGetHitByPlayer(); // 좀비 피격 함수 실행
                    break; // 관통이므로 이 좀비 체크는 끝내고 다음 좀비 루프로 넘어감
                }
            }
        }
    }

    private IEnumerator ShowArrowEffectRoutine()
    {
        if (arrowEffectObject == null) yield break;

        arrowEffectObject.transform.localPosition = _lookDirection * 0.5f; // 플레이어 살짝 정면에 배치

        if (_lookDirection == Vector3.right) arrowEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        else if (_lookDirection == Vector3.left) arrowEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        else if (_lookDirection == Vector3.up) arrowEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        else if (_lookDirection == Vector3.down) arrowEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);

        arrowEffectObject.SetActive(true);
        yield return new WaitForSeconds(arrowDuration);
        arrowEffectObject.SetActive(false);
    }

    private void TryBackstep()
    {
        if (_isMoving) return; // 이미 일반 타일 이동 중에는 백스텝 불가

        Debug.Log("인간 2스킬 백스텝 (K키)!");
        _backstepCooldownTimer = backstepCooldown; // 쿨타임 타이머 시동
        
        if (backstepBorderImage != null) backstepBorderImage.fillAmount = 0f;

        if (_backstepEffectCoroutine != null) StopCoroutine(_backstepEffectCoroutine);
        _backstepEffectCoroutine = StartCoroutine(ShowBackstepEffectRoutine());

        // 백스텝 방향 계산 (바라보는 방향의 반대 정반대 1칸)
        Vector3 backstepDirection = -_lookDirection;
        Vector3 targetPos = transform.position + backstepDirection;

        if (IsValidMoveTarget(targetPos))
        {
            StartCoroutine(MovePlayerBackstep(targetPos));
        }
        else
        {
            Debug.Log("뒤가 막혀있어 백스텝 이동이 불발되었습니다.");
        }
    }

    private IEnumerator ShowBackstepEffectRoutine()
    {
        if (backstepEffectObject == null) yield break;

        backstepEffectObject.transform.localPosition = Vector3.zero; 
        backstepEffectObject.SetActive(true);
        yield return new WaitForSeconds(backstepDuration);
        backstepEffectObject.SetActive(false);
    }

    private IEnumerator MovePlayerBackstep(Vector3 targetPos)
    {
        _isMoving = true;
        float elapsedTime = 0;
        _origPos = transform.position;
        _targetPos = targetPos;
        while (elapsedTime < backstepDuration)
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(_origPos, _targetPos, elapsedTime / backstepDuration);
            yield return null;
        }
        transform.position = _targetPos;
        
        UpdateGridSortingOrder();
        _isMoving = false;
    }

    private bool IsValidMoveTarget(Vector3 targetPos)
    {
        // 맵의 경계나 기믹 벽 충돌을 체크하려면 여기에 로직을 붙이세요.
        return true; 
    }

    private List<Vector2Int> GetFrontTiles(int myX, int myY, int range)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();
        Vector2Int directionInt = new Vector2Int(Mathf.RoundToInt(_lookDirection.x), Mathf.RoundToInt(_lookDirection.y));

        for (int i = 1; i <= range; i++)
        {
            tiles.Add(new Vector2Int(myX + directionInt.x * i, myY + directionInt.y * i));
        }
        return tiles;
    }

    private void UpdateGridSortingOrder()
    {
        if (_spriteRenderer == null) return;

        int currentGridY = Mathf.RoundToInt(transform.position.y);
        _spriteRenderer.sortingOrder = 100 - currentGridY;
        
        if (arrowEffectObject != null)
        {
            SpriteRenderer sr = arrowEffectObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = _spriteRenderer.sortingOrder + 1;
        }
        if (backstepEffectObject != null)
        {
            SpriteRenderer sr = backstepEffectObject.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = _spriteRenderer.sortingOrder + 1; 
        }
    }
}