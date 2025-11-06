using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Crea y posiciona displays en las 4 esquinas del tablero (World Space) para cada jugador.
/// - Usa anchors del tablero (Transform) en escena: TL, TR, BL, BR
/// - Mapea desde la perspectiva del jugador local (localPlayerIndex):
///   Local (yo) -> BR (bottom-right)
///   Siguiente -> TR (top-right)
///   Siguiente -> TL (top-left)
///   Anterior -> BL (bottom-left)
/// - Instancia un prefab de PlayerBoardDisplay por jugador y lo bindea al PlayerData.
/// - IMPORTANTE: En multiplayer, cada cliente debe configurar localPlayerIndex a su propio índice.
/// </summary>
public class BoardUIManager : MonoBehaviour
{
    [Header("Anchors del Tablero (World)")]
    public Transform anchorTopLeft;
    public Transform anchorTopRight;
    public Transform anchorBottomLeft;
    public Transform anchorBottomRight;

    [Header("Prefab y Escala")]
    public PlayerBoardDisplay playerDisplayPrefab;
    [Tooltip("Si está activo, fuerza la escala de cada display ignorando la del prefab")]
    public bool overrideScale = true;
    [Tooltip("Escala base aplicada a cada display instanciado (World Space). Recomendado: 0.001 a 0.01")]
    public Vector3 displayScale = new Vector3(0.0025f, 0.0025f, 0.0025f);

    [Header("Perspectiva del Jugador Local")]
    [Tooltip("Identificador del jugador local (playerId). En multiplayer, cada cliente debe poner aquí SU propio playerId.")]
    public int localPlayerIndex = 0;

    private readonly Dictionary<int, PlayerBoardDisplay> displaysByPlayerId = new Dictionary<int, PlayerBoardDisplay>();

    [Header("Inicialización")]
    [Tooltip("Si está activo, crea los displays al iniciar la escena. Si no, espera a CharacterSelection.")]
    public bool initializeOnStart = false;

    [Header("Debug")]
    [Tooltip("Imprime en consola cómo se mapean los jugadores a las esquinas")]
    public bool debugLayoutLogs = false;

    private void Start()
    {
        if (initializeOnStart)
        {
            TrySetup();
        }
        Subscribe();
        EnsurePreviewManager();
    }

    private void OnValidate()
    {
        // Aviso común: si la escala es muy grande, el Canvas World Space se verá enorme
        if (displayScale.x > 0.1f || displayScale.y > 0.1f || displayScale.z > 0.1f)
        {
            Debug.LogWarning("[BoardUIManager] displayScale es muy grande para World Space. Prueba con valores ~0.001 - 0.01");
        }
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnPlayerTurnChanged += HandleTurnChanged;
    }

    private void Unsubscribe()
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnPlayerTurnChanged -= HandleTurnChanged;
    }

    private void HandleTurnChanged(int currentIndex)
    {
        // No rotamos el tablero; la perspectiva del jugador local permanece fija
        // Solo refrescamos por si hay cambios visuales que actualizar
        RefreshLayout();
    }

    public void TrySetup()
    {
        if (GameManager.Instance == null || playerDisplayPrefab == null) return;
        var players = GameManager.Instance.GetAllPlayers();
        if (players == null || players.Count == 0) return;

        // Crear/actualizar displays
        foreach (var p in players)
        {
            if (p == null) continue;
            // Resolver ScriptableObject del personaje para pasar a Bind
            var charSO = FindCharacterSO(p.character);

            if (!displaysByPlayerId.TryGetValue(p.playerId, out var disp) || disp == null)
            {
                var go = Instantiate(playerDisplayPrefab, transform);
                if (overrideScale)
                {
                    go.transform.localScale = displayScale;
                }
                go.name = $"BoardDisplay_Player{p.playerId}_{p.playerName}";
                go.Bind(p, charSO);
                displaysByPlayerId[p.playerId] = go;
            }
            else
            {
                disp.Bind(p, charSO);
            }
        }

        RefreshLayout();
    }

    private void EnsurePreviewManager()
    {
        // Garantiza que exista un CardPreviewManager en escena para mostrar previews
        if (FindObjectOfType<CardPreviewManager>() != null) return;
        var go = new GameObject("CardPreviewManager");
        go.transform.SetParent(transform, worldPositionStays: false);
        go.AddComponent<CardPreviewManager>();
    }

    public void RefreshLayout()
    {
        if (GameManager.Instance == null) return;
        var players = GameManager.Instance.GetAllPlayers();
        if (players == null || players.Count == 0) return;
        if (anchorTopLeft == null || anchorTopRight == null || anchorBottomLeft == null || anchorBottomRight == null)
        {
            Debug.LogWarning("[BoardUIManager] Falta asignar uno o más anchors (TL/TR/BL/BR)");
            return;
        }

        // Resolver el índice EN LA LISTA del jugador local a partir de su playerId (localPlayerIndex)
        int localListIndex = ResolveLocalListIndex(players, localPlayerIndex);
        if (localListIndex < 0)
        {
            Debug.LogWarning($"[BoardUIManager] No se encontró en la lista un jugador con playerId={localPlayerIndex}. Se usará índice 0 como local.");
            localListIndex = 0;
        }

        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            if (p == null || !displaysByPlayerId.TryGetValue(p.playerId, out var disp) || disp == null) continue;

            // Calcular offset relativo al jugador local (perspectiva fija)
            // offset 0 = yo (local) -> BR
            // offset 1 = siguiente -> TR
            // offset 2 = siguiente -> TL
            // offset 3 = anterior -> BL
            int offset = (i - localListIndex);
            if (offset < 0) offset += players.Count;

            Transform anchor = GetAnchorForOffset(offset);
            if (anchor == null) continue;

            var t = disp.transform;
            t.SetParent(anchor, worldPositionStays: false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            if (overrideScale)
            {
                t.localScale = displayScale; // reasegurar escala solo si se fuerza
            }

            // Aplicar orientación de layout según la esquina
            var corner = GetCornerForOffset(offset);
            disp.SetOrientation(corner);

            if (debugLayoutLogs)
            {
                Debug.Log($"[BoardUIManager] PlayerId={p.playerId} ('{p.playerName}') en lista i={i} -> offset={offset} -> corner={corner}");
            }
        }
    }

    /// <summary>
    /// Devuelve el índice en la lista de 'players' cuyo playerId coincide con 'localPlayerId'.
    /// Si no existe, devuelve -1.
    /// </summary>
    private int ResolveLocalListIndex(List<PlayerData> players, int localPlayerId)
    {
        for (int idx = 0; idx < players.Count; idx++)
        {
            var pd = players[idx];
            if (pd != null && pd.playerId == localPlayerId)
                return idx;
        }
        return -1;
    }

    private Transform GetAnchorForOffset(int offset)
    {
        // offset 0 -> BR (yo, jugador local)
        // offset 1 -> TR (siguiente jugador en orden de turnos)
        // offset 2 -> TL (siguiente)
        // offset 3 -> BL (anterior, cierra el círculo)
        switch (offset)
        {
            case 0: return anchorBottomRight;  // Yo (local)
            case 1: return anchorTopRight;     // Siguiente
            case 2: return anchorTopLeft;      // Siguiente
            case 3: return anchorBottomLeft;   // Anterior
            default:
                // Si hay menos de 4 jugadores, doblar a posiciones disponibles: 0..3
                int wrapped = offset % 4;
                if (wrapped < 0) wrapped += 4;
                return GetAnchorForOffset(wrapped);
        }
    }

    private CharacterDataSO FindCharacterSO(CharacterType type)
    {
        // Búsqueda simple en assets cargados; si tienes una base de datos, reemplázalo
        var all = Resources.FindObjectsOfTypeAll<CharacterDataSO>();
        foreach (var so in all)
        {
            if (so != null && so.characterType == type)
                return so;
        }
        return null;
    }

    private PlayerBoardDisplay.Corner GetCornerForOffset(int offset)
    {
        int wrapped = offset % 4;
        if (wrapped < 0) wrapped += 4;
        switch (wrapped)
        {
            case 0: return PlayerBoardDisplay.Corner.BottomRight; // Yo (local)
            case 1: return PlayerBoardDisplay.Corner.TopRight;    // Siguiente
            case 2: return PlayerBoardDisplay.Corner.TopLeft;     // Siguiente
            case 3: return PlayerBoardDisplay.Corner.BottomLeft;  // Anterior
            default: return PlayerBoardDisplay.Corner.BottomRight;
        }
    }
}
