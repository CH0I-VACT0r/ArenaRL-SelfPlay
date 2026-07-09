using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AgentUI : MonoBehaviour
{
    [Header("Health Bar")]
    public Image hpFillImage;

    [Header("Skill UI (Index 0~3)")]
    public Image[] skillIcons;          // 스킬 원본 아이콘 이미지
    public Image[] cooldownOverlays;    // 반투명 검은색 덮개 (Filled 방식)
    public TextMeshProUGUI[] cooldownTexts; // 남은 시간 텍스트

    [Header("Status & Charge")]
    public TextMeshProUGUI statusText;
    public GameObject chargeBarContainer; // 배경을 포함하여 전체를 끄고 켜기 위한 부모 객체
    public Image chargeFillImage;

    // 스킬 아이콘 초기 세팅
    public void SetupSkillIcon(int index, Sprite icon)
    {
        if (index < 0 || index >= skillIcons.Length) return;

        if (icon != null)
        {
            skillIcons[index].sprite = icon;
            skillIcons[index].color = Color.white; // 아이콘이 보이도록 알파값 복구
        }
        else
        {
            // 아이콘이 없을 경우 투명 처리
            skillIcons[index].color = new Color(1f, 1f, 1f, 0f);
        }
    }

    // 체력 바 업데이트
    public void UpdateHealth(float currentHp, float maxHp)
    {
        if (hpFillImage != null)
        {
            hpFillImage.fillAmount = currentHp / maxHp;
        }
    }

    // 쿨타임 UI 매 프레임 업데이트
    public void UpdateCooldown(int index, float currentCooldown, float maxCooldown)
    {
        if (index < 0 || index >= cooldownOverlays.Length) return;

        if (currentCooldown > 0)
        {
            // 쿨타임 중: 검은색 오버레이 표시 및 텍스트 갱신
            cooldownOverlays[index].fillAmount = currentCooldown / maxCooldown;
            cooldownTexts[index].text = currentCooldown.ToString("F1"); // 소수점 첫째 자리까지 표기

            if (!cooldownOverlays[index].gameObject.activeSelf)
            {
                cooldownOverlays[index].gameObject.SetActive(true);
                cooldownTexts[index].gameObject.SetActive(true);
            }
        }
        else
        {
            // 쿨타임 완료: 오버레이 및 텍스트 숨김
            if (cooldownOverlays[index].gameObject.activeSelf)
            {
                cooldownOverlays[index].gameObject.SetActive(false);
                cooldownTexts[index].gameObject.SetActive(false);
            }
        }
    }

    public void UpdateStatusText(string text, Color color)
    {
        if (statusText != null)
        {
            statusText.text = text;
            statusText.color = color;
        }
    }
    public void UpdateChargeBar(float progress)
    {
        if (chargeBarContainer == null || chargeFillImage == null) return;

        // 진행도가 0보다 크고 1보다 작을 때만 차지 바 활성화
        if (progress > 0f && progress < 1f)
        {
            if (!chargeBarContainer.activeSelf) chargeBarContainer.SetActive(true);
            chargeFillImage.fillAmount = progress;
        }
        else
        {
            // 차지가 아닐 때는 바 숨김
            if (chargeBarContainer.activeSelf) chargeBarContainer.SetActive(false);
        }
    }
}