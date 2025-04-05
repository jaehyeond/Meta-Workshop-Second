using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InitBase : NetworkBehaviour
{
	protected bool _init = false;

	public virtual bool Init()
	{
		if (_init)
			return false;

		_init = true;
		return true;
	}

	private void Awake()
	{
		Init();
	}
}
