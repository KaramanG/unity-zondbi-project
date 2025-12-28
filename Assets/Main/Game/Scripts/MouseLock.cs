using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLock : MonoBehaviour
{
    public bool ShowCursorLock;
    private void Start()
    {
        ShowCursorLock = false;
        HideCursor();
    }
    private void Update()
    {
        if (IsCursorVisible()) ShowCursor();
        else HideCursor();
    }

    public bool IsCursorVisible()
    {
        return Input.GetKey(KeyCode.Tab) || ShowCursorLock;
}

    private void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
