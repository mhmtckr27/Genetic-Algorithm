using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToggleGroupPublicToggles : ToggleGroup
{
	public List<Toggle> GetToggles()
	{
		return m_Toggles;
	}

}
