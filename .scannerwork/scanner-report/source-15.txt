using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Zoom")]
    public float zoomSpeed = 6f;        // Sensibilidad de la rueda
    public float smoothZoomSpeed = 6f;  // Velocidad de suavizado del zoom
    public float minZoom = 5f;
    public float maxZoom = 10.8f;

    [Header("Movimiento")]
    public float dragSpeed = 0.5f;

    [Header("Límites del tablero (en unidades del mundo)")]
    public float boardWidth = 38.4f;
    public float boardHeight = 21.6f;

    private Camera cam;
    private Vector3 dragOrigin;
    private float targetZoom;            // Zoom objetivo
    private Vector3 targetPosition;      // Posición interpolada
    private Vector3 dragStartTargetPosition; // Posición objetivo al iniciar el drag

    void Start()
    {
        cam = Camera.main;
        // Iniciar en "zoom 0" (máximo alejamiento) y bloquear desplazamiento
        cam.orthographicSize = maxZoom;
        targetZoom = maxZoom;
        targetPosition = transform.position;
        ClampTargetPosition();
    }

    void Update()
    {
        HandleZoom();
        HandleDrag();

        // Interpolar suavemente tanto el zoom como la posición
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, Time.deltaTime * smoothZoomSpeed);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothZoomSpeed);
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.001f)
        {
            // Zoom hacia el puntero del ratón sin "teletransportar" la cámara
            // Fórmula ortográfica: P' = k*P + (1-k)*M, donde k = newSize/currentSize
            float currentSize = cam.orthographicSize;
            float newSize = Mathf.Clamp(currentSize - scroll * zoomSpeed, minZoom, maxZoom);
            Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);

            float k = newSize / currentSize;
            Vector3 newTargetPos = k * transform.position + (1f - k) * mouseWorld;

            targetZoom = newSize;
            targetPosition = newTargetPos;
            ClampTargetPosition();
        }
    }

    void HandleDrag()
    {
        // Solo permitir mover si hay algo de zoom (no ver todo el tablero)
        if (targetZoom >= maxZoom - 0.05f)
            return;

        if (Input.GetMouseButtonDown(1))
        {
            dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            dragStartTargetPosition = targetPosition; // anclar el inicio del drag
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 currentMouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 delta = (dragOrigin - currentMouseWorld) * dragSpeed;
            targetPosition = dragStartTargetPosition + delta;
            ClampTargetPosition();
        }
    }

    void ClampTargetPosition()
    {
        float camHeight = targetZoom * 2f;
        float camWidth = camHeight * cam.aspect;

        float halfBoardWidth = boardWidth / 2f;
        float halfBoardHeight = boardHeight / 2f;

        float limitX = halfBoardWidth - (camWidth / 2f);
        float limitY = halfBoardHeight - (camHeight / 2f);

        targetPosition.x = Mathf.Clamp(targetPosition.x, -limitX, limitX);
        targetPosition.y = Mathf.Clamp(targetPosition.y, -limitY, limitY);
    }
}