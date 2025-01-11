using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ironcow;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class ItemRoomSlot : UIListItem
{
	[SerializeField] private Image target;
	[SerializeField] private TMP_Text no;
	[SerializeField] private TMP_Text nickname;
	[SerializeField] private Image character;
	[SerializeField] private GameObject readyIcon;
	[SerializeField] private Sprite questionSprite;

	UnityAction<int> callback;

	public void SetItem(UserInfo userInfo, UnityAction<int> callback)
	{
		if (userInfo != null)
		{
			nickname.text = userInfo.nickname;
			OnChangeCharacter(userInfo.selectedCharacterRcode);
		}
		else
		{
			nickname.text = "";
			character.sprite = questionSprite;
		}

		target.gameObject.SetActive(false);
		readyIcon.SetActive(false);
		this.callback = callback;
	}

	public async void OnChangeCharacter(string rcode)
	{
		character.sprite = await ResourceManager.instance.LoadAsset<Sprite>(rcode, eAddressableType.Thumbnail);
	}

	public void OnTargetMark()
	{
		target.gameObject.SetActive(true);
	}

	public async void SetRoleIcon(eRoleType roleType)
	{
		target.sprite = await ResourceManager.instance.LoadAsset<Sprite>("Role_" + roleType.ToString(), eAddressableType.Thumbnail);
		target.gameObject.SetActive(true);
	}

	public void SetQuestionIcon()
	{
		target.sprite = questionSprite;
		target.gameObject.SetActive(true);
	}

	public void OnClickTestAddUser()
	{
		callback?.Invoke(transform.GetSiblingIndex());
	}

	public void SetReady(bool isReady)
	{
		readyIcon.SetActive(isReady);
	}
}
