using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private bool _isMoving;
    private Vector3 _origPos, _targetPos;

    [Header("이동 설정")]
    [Tooltip("한 칸 이동하는 데 걸리는 시간입니다. 값이 작아질수록 이동 속도가 빨라집니다.")]
    public float timeToMove = 0.16f;

    [Header("인간 방향별 스프라이트 등록")]
    [SerializeField] private Sprite spriteUp;    
    [SerializeField] private Sprite spriteDown;  
    [SerializeField] private Sprite spriteLeft;  
    [SerializeField] private Sprite spriteRight; 

    private SpriteRenderer _spriteRenderer; 

    [Header("1스킬: 휘두르기 (J키 - 방향별 3x2 / 2x3 유동 범위)")]
    [SerializeField] private Image swingBorderImage;
    [SerializeField] private Image swingCooldownImage;   
    [SerializeField] private Sprite swingWeaponEffectSprite;   
    public float swingDamage = 20f; 
    public float swingCooldown = 0.75f; 
    
    [HideInInspector] private float swingDuration = 0.25f; // 애니메이션 0.25초
    private float _swingCooldownTimer = 0f;
    private bool _isSwingReady = true;
    private byte _readyAlpha = 153; // 0.6f -> 255 * 0.6 = 153 (60%)

    [Header("2스킬: 카운터 (K키)")]
    [SerializeField] private GameObject counterEffectObject; 
    [SerializeField] private Image counterBorderImage;  // 2스킬 빨간 테두리 Image
    [SerializeField] private Image counterCoolDownImage; // 2스킬 안쪽 흰색 원형 게이지 Image
    [SerializeField] private Sprite counterWeaponEffectSprite;
    public float counterDamage = 40f;
    [SerializeField] private float counterDuration = 0.3f;
    [SerializeField] private float counterCooldown = 7.0f; 
    private float _counterCooldownTimer = 0f;
    private bool _isCounterReady = true;
    private bool _isInvincible = false; // 좀비 AI들이 체크하는 무적 상태 플래그

    private Vector3 _lookDirection = Vector3.down; 
    private Coroutine _swingEffectCoroutine;
    private Coroutine _counterEffectCoroutine;
    private GameObject _runtimeSwingWeaponObj;
    private GameObject _runtimeCounterWeaponObj;

    [Header("맵 설정")]
    [SerializeField] private UnityEngine.Tilemaps.Tilemap transparentWallTilemap;

    /// <summary>
    /// 외부 좀비 AI 스크립트에서 플레이어의 무적 여부를 확인할 수 있도록 공개하는 프로퍼티
    /// </summary>
    public bool IsInvincible
    {
        get { return _isInvincible; }
        set { _isInvincible = value; }
    }

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        CreateRuntimeWeaponEffects();
        SetupUIProperties();
    }

    private void SetupUIProperties()
    {
        if (swingBorderImage != null)
        {
            swingBorderImage.type = Image.Type.Filled;
            swingBorderImage.fillMethod = Image.FillMethod.Radial360;
            swingBorderImage.fillOrigin = (int)Image.Origin360.Top;
            swingBorderImage.fillClockwise = false; 
            swingBorderImage.fillAmount = 1f; 
        }

        if (counterBorderImage != null)
        {
            counterBorderImage.type = Image.Type.Filled;
            counterBorderImage.fillMethod = Image.FillMethod.Radial360;
            counterBorderImage.fillOrigin = (int)Image.Origin360.Top;
            counterBorderImage.fillClockwise = false; 
            counterBorderImage.fillAmount = 1f; 
        }

        if (swingCooldownImage != null) SetImageAlpha32(swingCooldownImage, _readyAlpha);
        if (counterCoolDownImage != null) SetImageAlpha32(counterCoolDownImage, _readyAlpha);
    }

    private bool IsTileBlocked(Vector3 targetPos)
    {
        if (transparentWallTilemap == null) return false;
        Vector3Int cellPosition = transparentWallTilemap.WorldToCell(targetPos);
        return transparentWallTilemap.HasTile(cellPosition);
    }

    void Update()
    {
        // 실시간 적용된 swingCooldown 수치로 UI 바 연산 진행
        if (_swingCooldownTimer > 0)
        {
            UpdateCooldownUI(ref _swingCooldownTimer, swingCooldown, swingBorderImage, swingCooldownImage);
        }
        else if (!_isSwingReady && Keyboard.current != null && !Keyboard.current.jKey.isPressed)
        {
            _isSwingReady = true;
            if (swingBorderImage != null) swingBorderImage.fillAmount = 1f; 
            if (swingCooldownImage != null) SetImageAlpha32(swingCooldownImage, _readyAlpha); 
        }

        // 실시간 적용된 counterCooldown 수치로 UI 바 연산 진행
        if (_counterCooldownTimer > 0)
        {
            UpdateCooldownUI(ref _counterCooldownTimer, counterCooldown, counterBorderImage, counterCoolDownImage);
        }

        // 1. 이동 처리
        if (Keyboard.current.wKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.up;
            ChangePlayerSprite(Vector3.up);
            if (!IsTileBlocked(transform.position + Vector3.up)) StartCoroutine(MovePlayer(Vector3.up));
        }
        else if (Keyboard.current.aKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.left;
            ChangePlayerSprite(Vector3.left);
            if (!IsTileBlocked(transform.position + Vector3.left)) StartCoroutine(MovePlayer(Vector3.left));
        }
        else if (Keyboard.current.sKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.down;
            ChangePlayerSprite(Vector3.down);
            if (!IsTileBlocked(transform.position + Vector3.down)) StartCoroutine(MovePlayer(Vector3.down));
        }
        else if (Keyboard.current.dKey.isPressed && !_isMoving) 
        {
            _lookDirection = Vector3.right;
            ChangePlayerSprite(Vector3.right);
            if (!IsTileBlocked(transform.position + Vector3.right)) StartCoroutine(MovePlayer(Vector3.right));
        }

        // 2. 1스킬 휘두르기 (J키)
        if (Keyboard.current.jKey.wasPressedThisFrame && _swingCooldownTimer <= 0)
        {
            TriggerSwingAttack();
        }

        // 3. 2스킬 카운터 (K키)
        if (Keyboard.current.kKey.wasPressedThisFrame && _counterCooldownTimer <= 0)
        {
            TriggerCounterAttack();
        }
    }

    // ★ UI 상태를 안전하게 제어하는 핵심 함수
    private void UpdateCooldownUI(ref float timer, float maxCooldown, Image borderImg, Image coolDownImg)
    {
        timer -= Time.deltaTime;

        if (timer > 0)
        {
            float fillRatio = (maxCooldown - timer) / maxCooldown;

            // 1. 빨간 테두리: 시계방향 차오름 연산
            if (borderImg != null && borderImg.type == Image.Type.Filled)
            {
                borderImg.fillAmount = fillRatio;
            }

            // 2. 안쪽 흰색 원: 투명도 25%로 낮춤
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
            // 3. 쿨타임이 딱 끝난 순간 복구
            timer = 0f;
            ResetUIState(borderImg, coolDownImg);
        }
    }

    // 평소 대기 상태로 UI를 되돌리는 깔끔한 분리 함수
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
        _isInvincible = false;
    }

    // 바라보는 방향에 맞춰 가로세로 범위가 정확히 정렬되어 들어가는 1스킬 연산
    private void TriggerSwingAttack()
    {
        _isSwingReady = false;
        _swingCooldownTimer = swingCooldown; 

        if (_swingEffectCoroutine != null) StopCoroutine(_swingEffectCoroutine);
        if (_runtimeSwingWeaponObj != null) _runtimeSwingWeaponObj.SetActive(false);

        _swingEffectCoroutine = StartCoroutine(InstantSwingRotationRoutine());

        Vector3 myPos = transform.position;
        float minX = myPos.x, maxX = myPos.x;
        float minY = myPos.y, maxY = myPos.y;

        // 위나 아래를 바라보고 있으면 가로 3, 세로 2 판정 크기 세팅
        if (_lookDirection == Vector3.up || _lookDirection == Vector3.down)
        {
            float attackLength = 2.0f; // 세로 지름
            float attackWidth = 1.0f;  // 가로 반경 (중심 기준 좌우 1칸씩 총 3칸)

            if (_lookDirection == Vector3.up)
            {
                minX = myPos.x - attackWidth; maxX = myPos.x + attackWidth;
                minY = myPos.y; maxY = myPos.y + attackLength;
            }
            else // Vector3.down
            {
                minX = myPos.x - attackWidth; maxX = myPos.x + attackWidth;
                minY = myPos.y - attackLength; maxY = myPos.y;
            }
        }
        // 좌우를 바라보고 있으면 가로 2, 세로 3 판정 크기 세팅
        else if (_lookDirection == Vector3.right || _lookDirection == Vector3.left)
        {
            float attackLength = 2.0f; // 가로 지름
            float attackWidth = 1.0f;  // 세로 반경 (중심 기준 위아래 1칸씩 총 3칸)

            if (_lookDirection == Vector3.right)
            {
                minX = myPos.x; maxX = myPos.x + attackLength;
                minY = myPos.y - attackWidth; maxY = myPos.y + attackWidth;
            }
            else // Vector3.left
            {
                minX = myPos.x - attackLength; maxX = myPos.x;
                minY = myPos.y - attackWidth; maxY = myPos.y + attackWidth;
            }
        }

        // 좀비 컴포넌트들을 탐색하여 범위 안에 있으면 피격 처리
        ZombieAI2_0[] zombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);
        foreach (ZombieAI2_0 zombie in zombies)
        {
            if (zombie == null) continue;
            Vector3 zombiePos = zombie.transform.position;
            
            if (zombiePos.x >= minX && zombiePos.x <= maxX && zombiePos.y >= minY && zombiePos.y <= maxY)
            {
                zombie.OnGetHitByPlayer(swingDamage, false);
            }
        }
    }

    private void TriggerCounterAttack()
    {
        _isCounterReady = false;
        _counterCooldownTimer = counterCooldown; 

        if (_counterEffectCoroutine != null) StopCoroutine(_counterEffectCoroutine);
        if (_runtimeCounterWeaponObj != null) _runtimeCounterWeaponObj.SetActive(false);

        _counterEffectCoroutine = StartCoroutine(ShowCounterEffectRoutine());

        Vector3 myPos = transform.position;
        float minX = myPos.x, maxX = myPos.x;
        float minY = myPos.y, maxY = myPos.y;
        
        float counterLength = 2.0f; 
        float counterWidth = 0.4f;  

        if (_lookDirection == Vector3.right)
        {
            minX = myPos.x; maxX = myPos.x + counterLength;
            minY = myPos.y - counterWidth; maxY = myPos.y + counterWidth;
        }
        else if (_lookDirection == Vector3.left)
        {
            minX = myPos.x - counterLength; maxX = myPos.x;
            minY = myPos.y - counterWidth; maxY = myPos.y + counterWidth;
        }
        else if (_lookDirection == Vector3.up)
        {
            minX = myPos.x - counterWidth; maxX = myPos.x + counterWidth;
            minY = myPos.y; maxY = myPos.y + counterLength;
        }
        else if (_lookDirection == Vector3.down)
        {
            minX = myPos.x - counterWidth; maxX = myPos.x + counterWidth;
            minY = myPos.y - counterLength; maxY = myPos.y;
        }

        ZombieAI2_0[] zombies = Object.FindObjectsByType<ZombieAI2_0>(FindObjectsSortMode.None);
        foreach (ZombieAI2_0 zombie in zombies)
        {
            if (zombie == null) continue;
            Vector3 zombiePos = zombie.transform.position;
            
            if (zombiePos.x >= minX && zombiePos.x <= maxX && zombiePos.y >= minY && zombiePos.y <= maxY)
            {
                Vector3 zombieDir = zombie.GetCurrentDirection(); 

                // 서로 마주보고 있는 경우 카운터 성립
                if (Vector3.Distance(_lookDirection + zombieDir, Vector3.zero) < 0.1f)
                {
                    zombie.OnGetHitByPlayer(counterDamage, true);
                }
            }
        }
    }

    private void SetImageAlpha32(Image img, byte alpha)
    {
        if (img == null) return;
        Color32 c32 = img.color;
        c32.a = alpha;
        img.color = c32;
    }

    private void CreateRuntimeWeaponEffects()
    {
        string playerSortingLayer = _spriteRenderer != null ? _spriteRenderer.sortingLayerName : "Default";
        int playerSortingOrder = _spriteRenderer != null ? _spriteRenderer.sortingOrder : 0;

        if (swingWeaponEffectSprite != null)
        {
            _runtimeSwingWeaponObj = new GameObject("RuntimeSwingWeaponEffect", typeof(SpriteRenderer));
            _runtimeSwingWeaponObj.transform.SetParent(transform, false); 
            SpriteRenderer sr = _runtimeSwingWeaponObj.GetComponent<SpriteRenderer>();
            sr.sprite = swingWeaponEffectSprite;
            
            sr.sortingLayerName = playerSortingLayer;
            sr.sortingOrder = playerSortingOrder + 5;
            
            _runtimeSwingWeaponObj.SetActive(false);
        }

        if (counterWeaponEffectSprite != null)
        {
            _runtimeCounterWeaponObj = new GameObject("RuntimeCounterWeaponEffect", typeof(SpriteRenderer));
            _runtimeCounterWeaponObj.transform.SetParent(transform, false); 
            SpriteRenderer sr = _runtimeCounterWeaponObj.GetComponent<SpriteRenderer>();
            sr.sprite = counterWeaponEffectSprite;
            
            sr.sortingLayerName = playerSortingLayer;
            sr.sortingOrder = playerSortingOrder + 5;
            
            _runtimeCounterWeaponObj.SetActive(false);
        }
    }

    private IEnumerator InstantSwingRotationRoutine()
    {
        if (_runtimeSwingWeaponObj == null) yield break;

        float baseAngle = 0f;
        if (_lookDirection == Vector3.right) baseAngle = 0f;
        else if (_lookDirection == Vector3.up) baseAngle = 90f;
        else if (_lookDirection == Vector3.left) baseAngle = 180f;
        else if (_lookDirection == Vector3.down) baseAngle = 270f;

        _runtimeSwingWeaponObj.SetActive(true);

        SetEffectTransform(baseAngle - 50f); yield return new WaitForSeconds(swingDuration / 3f);
        SetEffectTransform(baseAngle); yield return new WaitForSeconds(swingDuration / 3f);
        SetEffectTransform(baseAngle + 50f); yield return new WaitForSeconds(swingDuration / 3f);

        _runtimeSwingWeaponObj.SetActive(false);
    }

    private void SetEffectTransform(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * 1.0f;
        _runtimeSwingWeaponObj.transform.localPosition = offset;
        _runtimeSwingWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private IEnumerator ShowCounterEffectRoutine()
    {
        if (_runtimeCounterWeaponObj == null) yield break;

        _runtimeCounterWeaponObj.transform.localPosition = _lookDirection * 1.2f;

        if (_lookDirection == Vector3.right) _runtimeCounterWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        else if (_lookDirection == Vector3.left) _runtimeCounterWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        else if (_lookDirection == Vector3.up) _runtimeCounterWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        else if (_lookDirection == Vector3.down) _runtimeCounterWeaponObj.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);

        _runtimeCounterWeaponObj.SetActive(true);
        yield return new WaitForSeconds(counterDuration);
        _runtimeCounterWeaponObj.SetActive(false);
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
        while (elapsedTime < timeToMove) 
        {
            elapsedTime += Time.deltaTime;
            transform.position = Vector3.Lerp(_origPos, _targetPos, elapsedTime / timeToMove);
            yield return null;
        }
        transform.position = _targetPos;
       
        _isMoving = false;
    }

    // 외부 발판 트리거 전용 안전한 텔레포트 제어 함수
    public void TeleportTo(Vector3 targetNewPos)
    {
        StopAllCoroutines(); // 1. 돌고 있던 플레이어 칸 이동 코루틴 강제 정지
        _isMoving = false;    // 2. 이동 상태 플래그 초기화

        // 3. 목적지 좌표 세팅 및 동기화 작업
        transform.position = targetNewPos;
        _origPos = targetNewPos;
        _targetPos = targetNewPos;

        Debug.Log($"[TeleportTo] 플레이어가 그리드 예외 처리와 함께 {targetNewPos} 위치로 완벽 텔포되었습니다.");
    }

    /// <summary>
    /// 좀비가 플레이어를 성공적으로 공격했을 때 호출하는 피격 처리 메서드
    /// </summary>
    /// <param name="damage">좀비가 가하는 데미지 수치</param>
    public void OnGetHitByZombie(float damage)
    {
        if (_isInvincible) return;

        // TODO: 프로젝트 내 체력 시스템 구조에 맞춰 연산 추가 (예: PlayerHP -= damage;)
        Debug.Log($"[PlayerMovement] 좀비에게 공격당해 {damage}만큼 피격되었습니다.");

        // 피격 후 0.5초간 임시 무적 부여
        StartCoroutine(TempInvincibleRoutine(0.5f)); 
    }

    /// <summary>
    /// 피격 또는 특정 이벤트 후 임시로 무적 상태를 제어해주는 루틴
    /// </summary>
    private IEnumerator TempInvincibleRoutine(float duration)
    {
        _isInvincible = true;
        yield return new WaitForSeconds(duration);
        _isInvincible = false;
    }
}