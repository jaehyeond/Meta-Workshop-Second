// using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Assets.Scripts.Resource;
using UnityEngine;

public class DamageFont : MonoBehaviour
{
	private TextMeshPro _damageText;
    private ResourceManager _resourceManager;
	private bool _isCritical;
	
	public void SetInfo(Vector2 pos, float damage = 0, Transform parent = null, bool isCritical = false, ResourceManager resourceManager = null)
	{
		_damageText = GetComponent<TextMeshPro>();
		_damageText.sortingOrder = SortingLayers.PROJECTILE;
		_resourceManager = resourceManager;
		_isCritical = isCritical;

		transform.position = pos;

		if (damage < 0)
		{
			_damageText.color = Util.HexToColor("4EEE6F");
		}
		else if (isCritical)
		{
			_damageText.color = Util.HexToColor("EFAD00");
			_damageText.fontSize += 2;  // 크리티컬은 폰트 크기 증가
		}
		else
		{
			_damageText.color = Color.white; // 일반 데미지는 흰색으로 변경
		}

		_damageText.text = $"{Mathf.Abs(damage)}";
		_damageText.alpha = 1;

		if (parent != null)
			GetComponent<MeshRenderer>().sortingOrder = SortingLayers.DAMAGE_FONT;

		// DoAnimation();
	}

	// private void DoAnimation()
	// {
	// 	Sequence seq = DOTween.Sequence();
		
	// 	// 초기 상태 설정
	// 	transform.localScale = Vector3.zero;
		
	// 	if (_isCritical)
	// 	{
	// 		// 크리티컬 데미지는 더 화려한 애니메이션
	// 		seq.Append(transform.DOScale(1.5f, 0.2f).SetEase(Ease.OutBack))
	// 		   .Join(transform.DOMove(transform.position + new Vector3(0, 0.5f, 0), 0.2f).SetEase(Ease.OutCubic))
	// 		   .Append(transform.DOPunchRotation(new Vector3(0, 0, 15), 0.3f, 2))
	// 		   .Join(transform.DOScale(1.2f, 0.3f))
	// 		   .Append(transform.DOMove(transform.position + new Vector3(0, 0.8f, 0), 0.5f).SetEase(Ease.OutCubic))
	// 		   .Join(_damageText.DOFade(0, 0.5f).SetEase(Ease.InQuint))
	// 		   .OnComplete(() =>
	// 		   {
	// 			   _resourceManager.Destroy(gameObject);
	// 		   });
	// 	}
	// 	else
	// 	{
	// 		// 일반 데미지 - 박히는 느낌으로 변경
	// 		seq.Append(transform.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad))
	// 		   .Join(_damageText.DOColor(new Color(_damageText.color.r, _damageText.color.g, _damageText.color.b, 0.9f), 0.1f))
	// 		   .Append(transform.DOScale(0.9f, 0.1f).SetEase(Ease.OutBounce))  // 튕기는 느낌으로 약간 줄어듦
	// 		   .Append(transform.DOScale(1.0f, 0.1f))  // 다시 원래 크기로
	// 		   .Append(transform.DOMove(transform.position + new Vector3(0, 0.4f, 0), 0.5f).SetEase(Ease.OutCubic))  // 천천히 위로 올라감
	// 		   .Join(_damageText.DOFade(0, 0.5f).SetEase(Ease.InQuint))
	// 		   .OnComplete(() =>
	// 		   {
	// 			   _resourceManager.Destroy(gameObject);
	// 		   });
	// 	}
	// }
}
