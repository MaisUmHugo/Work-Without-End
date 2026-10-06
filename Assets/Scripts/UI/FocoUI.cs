using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class FocoUI
{
    public static void SelecionarPrimeiroBotao(GameObject painel)
    {
        if (EventSystem.current == null || painel == null || !painel.activeInHierarchy) return;

        foreach (Button botao in painel.GetComponentsInChildren<Button>(true))
        {
            if (!botao.isActiveAndEnabled || !botao.IsInteractable()) continue;
            EventSystem.current.SetSelectedGameObject(botao.gameObject);
            return;
        }
    }
}
