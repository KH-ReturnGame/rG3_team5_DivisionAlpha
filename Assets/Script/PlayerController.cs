using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.InputSystem;

public enum PlayerMode { Chairman, Archer }

public class PlayerController : MonoBehaviour
{
    [Header("=== 모드 설정 ===")]
    public PlayerMode currentMode = PlayerMode.Chairman;

    private bool _isMoving;
    private Vector3 _origPos, _targetPos;
    private float _timeToMove = 0.13f;
    private Vector3 _lookDirection = Vector3.down; 
    private SpriteRenderer _spriteRenderer; 
    private Coroutine _attackEffectCoroutine;
    private Coroutine _arrowEffectCoroutine;    // 👈 에러 해결을 위해 누락된 코루틴 변수 추가 완료!
    private Coroutine _backstepEffectCoroutine;

    [Header("인간 방향별 스프라이트 등록")]
    [SerializeField] private Sprite spriteUp;    
    [SerializeField] private Sprite spriteDown;  
    [SerializeField] private Sprite spriteLeft;  
    [SerializeField] private Sprite spriteRight; 

    // ==========================================
    // [체어맨 전용 변수]
    // ==========================================
    [Header("--- 체어맨 스킬 ---")]
    [SerializeField] private GameObject swingEffectObject; 
    [SerializeField] private Image swingBorderImage;   
    [SerializeField] private Image swingCoolDownImage;  
    private float swingDuration = 0.25f; 
    private float swingCooldown = 0.75f; 
    private float _swingCooldownTimer = 0f;

    [SerializeField] private GameObject counterEffectObject; 
    [SerializeField] private Image counterBorderImage;  
    [SerializeField] private Image counterCoolDownImage; 
    [SerializeField] private float counterDuration = 0.3f;
    [SerializeField] private float counterCooldown = 7.0f; 
    private float _counterCooldownTimer = 0f;

    // ==========================================
    // [아처 전용 변수]
    // ==========================================
    [Header("--- 아처 스킬 ---")]
    [SerializeField] private GameObject arrowEffectObject; 
    [SerializeField] private Image arrowBorderImage;   
    [SerializeField] private Image arrowCoolDownImage;   
    [SerializeField] private float arrowDuration = 0.2f; 
    [SerializeField] private float arrowCooldown = 1.0f; 
    private float _arrowCooldownTimer = 0f;
    [SerializeField] private int arrowRange = 5;          

    [SerializeField] private GameObject backstepEffectObject; 
    [SerializeField] private Image backstepBorderImage;  
    [SerializeField] private Image backstepCoolDownImage; 
    [SerializeField] private float backstepDuration = 0.3f; 
    [SerializeField] private float backstepCooldown = 5.0f; 
    private float _backstepCooldownTimer = 0f;

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        
        // 모든 이펙트 오브젝트 초기 비활성화
        if (swingEffectObject != null) swingEffectObject.SetActive(false);
        if (counterEffectObject != null) counterEffectObject.SetActive(false);
        if (arrowEffectObject != null) arrowEffectObject.SetActive(false);
        if (backstepEffectObject != null) backstepEffectObject.SetActive(false);

        // UI 초기화
        ResetUIState(swingBorderImage, swingCoolDownImage);
        ResetUIState(counterBorderImage, counterCoolDownImage);
        ResetUIState(arrowBorderImage, arrowCoolDownImage);
        ResetUIState(backstepBorderImage, backstepCoolDownImage);

        UpdateGridSortingOrder();
    }

    void Update()
    {
        // 1. 모든 쿨타임 타이머 상시 업데이트 (모드가 바뀌어도 배경에서 쿨타임은 흐르도록 처리)
        if (_swingCooldownTimer > 0) UpdateCooldownUI(ref _swingCooldownTimer, swingCooldown, swingBorderImage, swingCoolDownImage);
        if (_counterCooldownTimer > 0) UpdateCooldownUI(ref _counterCooldownTimer, counterCooldown, counterBorderImage, counterCoolDownImage);
        if (_arrowCooldownTimer > 0) UpdateCooldownUI(ref _arrowCooldownTimer, arrowCooldown, arrowBorderImage, arrowCoolDownImage);
        if (_backstepCooldownTimer > 0) UpdateCooldownUI(ref _backstepCooldownTimer, backstepCooldown, backstepBorderImage, backstepCoolDownImage);

        // 테스트용 모드 전환 키 (예: Tab 키를 누르면 직업이 바뀜)
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            SwitchMode(currentMode == PlayerMode.Chairman ? PlayerMode.Archer : PlayerMode.Chairman);
        }

        // 2. 공통 이동 처리
        HandleMovementInput();

        // 3. 모드별 스킬 처리
        HandleSkillInput();
    }

    // 모드 변경 함수 (외부 매니저나 이벤트에서 호출 가능)
    public void SwitchMode(PlayerMode newMode)
    {
        currentMode = newMode;
        Debug.Log($"플레이어 직업 변경 -> {currentMode}");
    }

    private void HandleMovementInput()
    {
        if (_isMoving) return;

        if (Keyboard.current.wKey.isPressed) { MoveInDirection(Vector3.up); }
        else if (Keyboard.current.aKey.isPressed) { MoveInDirection(Vector3.left); }
        else if (Keyboard.current.sKey.isPressed) { MoveInDirection(Vector3.down); }
        else if (Keyboard.current.dKey.isPressed) { MoveInDirection(Vector3.right); }
    }

    private void MoveInDirection(Vector3 direction)
    {
        _lookDirection = direction;
        ChangePlayerSprite(direction);
        StartCoroutine(MovePlayer(direction));
    }

    private void HandleSkillInput()
    {
        if (currentMode == PlayerMode.Chairman)
        {
            // 체어맨 스킬 입력
            if (Keyboard.current.jKey.wasPressedThisFrame && _swingCooldownTimer <= 0) TrySwingAttack();
            if (Keyboard.current.kKey.wasPressedThisFrame && _counterCooldownTimer <= 0) TryCounterAttack();
        }
        else if (currentMode == PlayerMode.Archer)
        {
            // 아처 스킬 입력
            if (Keyboard.current.jKey.wasPressedThisFrame && _arrowCooldownTimer <= 0) TryArrowAttack();
            if (Keyboard.current.kKey.wasPressedThisFrame && _backstepCooldownTimer <= 0) TryBackstep();
        }
    }

    // ==========================================
    // [공통 로직: UI, 이동, 정렬]
    // ==========================================
    private void UpdateCooldownUI(ref float timer, float maxCooldown, Image borderImg, Image coolDownImg)
    {
        timer -= Time.deltaTime;
        if (timer > 0)
        {
            float fillRatio = (maxCooldown - timer) / maxCooldown;
            if (borderImg != null && borderImg.type == Image.Type.Filled) borderImg.fillAmount = fillRatio;
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
            timer = 0f;
            ResetUIState(borderImg, coolDownImg);
        }
    }

    private void ResetUIState(Image borderImg, Image coolDownImg)
    {
        if (borderImg != null) borderImg.fillAmount = 1f;
        if (coolDownImg != null)
        {
            Color cColor = coolDownImg.color;
            cColor.a = 0.6f; 
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

    private void UpdateGridSortingOrder()
    {
        if (_spriteRenderer == null) return;
        int currentGridY = Mathf.RoundToInt(transform.position.y);
        _spriteRenderer.sortingOrder = 100 - currentGridY;
        
        SetEffectSorting(swingEffectObject);
        SetEffectSorting(counterEffectObject);
        SetEffectSorting(arrowEffectObject);
        SetEffectSorting(backstepEffectObject);
    }

    private void SetEffectSorting(GameObject effectObj)
    {
        if (effectObj != null)
        {
            SpriteRenderer sr = effectObj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = _spriteRenderer.sortingOrder + 1;
        }
    }

    // ==========================================
    // [체어맨 스킬 로직]
    // ==========================================
    private void TrySwingAttack()
    {
        Debug.Log("인간 1스킬 일반 휘두르기 공격 (J키)!");
        _swingCooldownTimer = swingCooldown;
        if (swingBorderImage != null) swingBorderImage.fillAmount = 0f;

        if (_attackEffectCoroutine != null) StopCoroutine(_attackEffectCoroutine);
        _attackEffectCoroutine = StartCoroutine(InstantSwingRotationRoutine());

        int myX = Mathf.RoundToInt(transform.position.x);
        int myY = Mathf.RoundToInt(transform.position.y);
        List<Vector2Int> targetTiles = GetFrontThreeTiles(myX, myY);

        ZombieAIWithCounter[] zombies = Object.FindObjectsByType<ZombieAIWithCounter>(FindObjectsSortMode.None);
        foreach (ZombieAIWithCounter zombie in zombies)
        {
            int zombieX = Mathf.RoundToInt(zombie.transform.position.x);
            int zombieY = Mathf.RoundToInt(zombie.transform.position.y);
            foreach (Vector2Int tile in targetTiles)
            {
                if (zombieX == tile.x && zombieY == tile.y)
                {
                    Debug.Log($"좀비가 1스킬(휘두르기)에 맞아 데미지를 입음!");
                    break; 
                }
            }
        }
    }

    private IEnumerator InstantSwingRotationRoutine()
    {
        if (swingEffectObject == null) yield break;
        float baseAngle = 0f;
        if (_lookDirection == Vector3.right) baseAngle = 0f;
        else if (_lookDirection == Vector3.up) baseAngle = 90f;
        else if (_lookDirection == Vector3.left) baseAngle = 180f;
        else if (_lookDirection == Vector3.down) baseAngle = 270f;

        swingEffectObject.SetActive(true);
        SetEffectTransform(baseAngle - 50f);
        yield return new WaitForSeconds(swingDuration / 3f);
        SetEffectTransform(baseAngle);
        yield return new WaitForSeconds(swingDuration / 3f);
        SetEffectTransform(baseAngle + 50f);
        yield return new WaitForSeconds(swingDuration / 3f);
        swingEffectObject.SetActive(false);
    }

    private void SetEffectTransform(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * 1.0f;
        swingEffectObject.transform.localPosition = offset;
        swingEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void TryCounterAttack()
    {
        Debug.Log("인간 2스킬 카운터 스킬 발동 (K키)!");
        _counterCooldownTimer = counterCooldown;
        if (counterBorderImage != null) counterBorderImage.fillAmount = 0f;

        if (_attackEffectCoroutine != null) StopCoroutine(_attackEffectCoroutine);
        _attackEffectCoroutine = StartCoroutine(ShowCounterEffectRoutine());

        Vector3 attackOneStepPos = transform.position + _lookDirection;
        Vector3 attackTwoStepPos = transform.position + (_lookDirection * 2);
        int t1X = Mathf.RoundToInt(attackOneStepPos.x); int t1Y = Mathf.RoundToInt(attackOneStepPos.y);
        int t2X = Mathf.RoundToInt(attackTwoStepPos.x); int t2Y = Mathf.RoundToInt(attackTwoStepPos.y);

        ZombieAIWithCounter[] zombies = Object.FindObjectsByType<ZombieAIWithCounter>(FindObjectsSortMode.None);
        foreach (ZombieAIWithCounter zombie in zombies)
        {
            int zombieX = Mathf.RoundToInt(zombie.transform.position.x);
            int zombieY = Mathf.RoundToInt(zombie.transform.position.y);
            if ((zombieX == t1X && zombieY == t1Y) || (zombieX == t2X && zombieY == t2Y))
            {
                zombie.OnGetHitByPlayer();
                break; 
            }
        }
    }

    private IEnumerator ShowCounterEffectRoutine()
    {
        if (counterEffectObject == null) yield break;
        counterEffectObject.transform.localPosition = _lookDirection * 1.5f;
        if (_lookDirection == Vector3.right) counterEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        else if (_lookDirection == Vector3.left) counterEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        else if (_lookDirection == Vector3.up) counterEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        else if (_lookDirection == Vector3.down) counterEffectObject.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);

        counterEffectObject.SetActive(true);
        yield return new WaitForSeconds(counterDuration);
        counterEffectObject.SetActive(false);
    }

    private List<Vector2Int> GetFrontThreeTiles(int myX, int myY)
    {
        List<Vector2Int> tiles = new List<Vector2Int>();
        if (_lookDirection == Vector3.up)
        {
            tiles.Add(new Vector2Int(myX - 1, myY + 1)); tiles.Add(new Vector2Int(myX, myY + 1)); tiles.Add(new Vector2Int(myX + 1, myY + 1));
        }
        else if (_lookDirection == Vector3.down)
        {
            tiles.Add(new Vector2Int(myX - 1, myY - 1)); tiles.Add(new Vector2Int(myX, myY - 1)); tiles.Add(new Vector2Int(myX + 1, myY - 1));
        }
        else if (_lookDirection == Vector3.left)
        {
            tiles.Add(new Vector2Int(myX - 1, myY + 1)); tiles.Add(new Vector2Int(myX - 1, myY)); tiles.Add(new Vector2Int(myX - 1, myY - 1));
        }
        else if (_lookDirection == Vector3.right)
        {
            tiles.Add(new Vector2Int(myX + 1, myY + 1)); tiles.Add(new Vector2Int(myX + 1, myY)); tiles.Add(new Vector2Int(myX + 1, myY - 1));
        }
        return tiles;
    }

    // ==========================================
    // [아처 스킬 로직]
    // ==========================================
    private void TryArrowAttack()
    {
        Debug.Log("인간 1스킬 즉시 관통 화살 발사 (J키)!");
        _arrowCooldownTimer = arrowCooldown;
        if (arrowBorderImage != null) arrowBorderImage.fillAmount = 0f;

        if (_arrowEffectCoroutine != null) StopCoroutine(_arrowEffectCoroutine);
        _arrowEffectCoroutine = StartCoroutine(ShowArrowEffectRoutine());

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
                    zombie.OnGetHitByPlayer();
                    break; 
                }
            }
        }
    }

    private IEnumerator ShowArrowEffectRoutine()
    {
        if (arrowEffectObject == null) yield break;
        arrowEffectObject.transform.localPosition = _lookDirection * 0.5f;
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
        if (_isMoving) return;
        Debug.Log("인간 2스킬 백스텝 (K키)!");
        _backstepCooldownTimer = backstepCooldown;
        if (backstepBorderImage != null) backstepBorderImage.fillAmount = 0f;

        if (_backstepEffectCoroutine != null) StopCoroutine(_backstepEffectCoroutine);
        _backstepEffectCoroutine = StartCoroutine(ShowBackstepEffectRoutine());

        Vector3 backstepDirection = -_lookDirection;
        Vector3 targetPos = transform.position + backstepDirection;

        if (IsValidMoveTarget(targetPos)) StartCoroutine(MovePlayerBackstep(targetPos));
        else Debug.Log("뒤가 막혀있어 백스텝 이동이 불발되었습니다.");
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
}