# DiceRollerUI – Lanzamiento de dado centrado

Este componente muestra una ventana centrada con una animación de dado y devuelve el resultado. Pausa el temporizador de turno automáticamente cuando se usa a través del `GameManager`.

## Qué incluye
- Overlay con fade-in/out que bloquea la interacción mientras se lanza el dado.
- Ventana con animación de “pop” al aparecer.
- Agitado y cambio rápido de caras durante ~1s.
- Muestra el resultado (con sprite de cara si existe, o número como respaldo).
- Callback y evento para obtener el valor final.

## Configuración en la escena
1. En tu `Canvas` principal, crea un GameObject llamado `DiceRoller` (o similar) y agrega el script `DiceRollerUI`.
2. Crea una jerarquía básica:
   - DiceRoller (GameObject con `DiceRollerUI`)
     - Overlay (Image + `CanvasGroup`) – color negro con alpha ~0.5
     - Window (Panel, anclado al centro)
       - DiceImage (Image)
       - ResultText (TextMeshProUGUI, grande y centrado)
3. Asigna en el Inspector del `DiceRollerUI`:
   - `Overlay Group`: el `CanvasGroup` del Overlay.
   - `Window Root`: el RectTransform de la ventana.
   - `Dice Image`: la Image de la cara del dado (opcional si usarás modo numérico).
   - `Result Text`: el TMP para mostrar el número en modo respaldo.
   - (Opcional) `Dice Face Sprites`: arrastra 1..N sprites (índice 0 = cara 1).

Sugerencia de sprites (opcional):
- Coloca tus sprites en `Assets/Resources/UI/Dice/dice1.png` .. `dice6.png` y asígnalos en el array.

## Integración con GameManager
1. En el `GameManager` de la escena, asigna la referencia al `DiceRollerUI` en el campo "UI del lanzamiento de dado".
2. Llama desde código:
```csharp
GameManager.Instance.RollDice(6, result => {
    Debug.Log($"Resultado: {result}");
    // TODO: aplicar efectos según el resultado
});
```
3. Para pruebas rápidas, presiona la tecla R (1d6). El GameManager pausa el temporizador de turno durante la animación y lo reanuda al terminar.

## API rápida
- DiceRollerUI:
  - `void RollDice(int sides = 6, Action<int> onComplete = null)`
- GameManager:
  - `void RollDice(int sides = 6, Action<int> onResult = null)` – recomendado; pausa/reanuda el timer y emite `OnDiceRolled`.
  - `event Action<int> OnDiceRolled` – se dispara cuando hay resultado.

## Ajustes de animación
- `overlayFadeDuration`: velocidad de fade del overlay.
- `windowPopScale` y `windowPopDuration`: intensidad/tiempo del efecto de aparición.
- `rollDuration`: cuánto dura el “agitado”.
- `faceShuffleInterval`: cada cuánto cambia de cara durante el agitado.
- `resultHoldDuration`: cuánto tiempo se muestra el resultado antes de ocultar.

## Notas
- Si no se asignan sprites para todas las caras necesarias, el componente mostrará el número en modo texto como respaldo.
- Las animaciones usan tiempo no escalado, por lo que no dependen de tu timeScale.
- Requiere DOTween y TextMeshPro.
