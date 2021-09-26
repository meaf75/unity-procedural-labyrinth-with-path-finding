using UnityEngine;
using UnityEngine.EventSystems;

public class UITouchHandler : MonoBehaviour, IPointerClickHandler {

	private GameMaster gameMaster;
	
	public void Initialize(GameMaster _gameMaster) {
		gameMaster = _gameMaster;
	}

	
	public void OnPointerClick(PointerEventData eventData) {
		gameMaster.HandleTouch(eventData.position);
	}
}
