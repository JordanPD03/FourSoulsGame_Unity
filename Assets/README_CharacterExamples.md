# Ejemplo de Configuración de Personajes

## Isaac (Personaje Base)

### Stats Iniciales
- **HP**: 2
- **Monedas**: 3
- **Ataque**: 1

### Objetos Eternos
- **The D6**: 
  - Tipo: Active Item
  - Efecto: Reroll de ítems pasivos/activos del jugador

### Habilidad
"Al inicio de tu turno, puedes usar The D6 sin costo adicional"

---

## Magdalene

### Stats Iniciales
- **HP**: 3
- **Monedas**: 3
- **Ataque**: 1

### Objetos Eternos
- **Yum Heart**:
  - Tipo: Active Item
  - Efecto: Gana 1 HP

### Habilidad
"Empieza con 3 HP en lugar de 2"

---

## Cain

### Stats Iniciales
- **HP**: 2
- **Monedas**: 4
- **Ataque**: 1

### Objetos Eternos
- **Lucky Foot**:
  - Tipo: Passive Item
  - Efecto: +1 en todas las tiradas de dado

### Habilidad
"Empieza con 4 monedas y Lucky Foot"

---

## Judas

### Stats Iniciales
- **HP**: 1
- **Monedas**: 3
- **Ataque**: 1

### Objetos Eternos
- **Book of Belial**:
  - Tipo: Active Item
  - Efecto: +2 ATK este turno

### Habilidad
"Empieza con 1 HP pero con Book of Belial"

---

## Blue Baby (???)

### Stats Iniciales
- **HP**: 3
- **Monedas**: 3
- **Ataque**: 1

### Objetos Eternos
- **The Poop**:
  - Tipo: Active Item
  - Efecto: Coloca una barrera

### Habilidad
"No puede tener más de 3 HP. Las curaciones se convierten en monedas"

---

## Eve

### Stats Iniciales
- **HP**: 2
- **Monedas**: 3
- **Ataque**: 1

### Objetos Eternos
- **Razor Blade**:
  - Tipo: Active Item
  - Efecto: Pierde 1 HP, gana +1 ATK permanente
- **Whore of Babylon**:
  - Tipo: Passive Item
  - Efecto: +2 ATK cuando tienes 1 HP o menos

### Habilidad
"Empieza con Razor Blade y Whore of Babylon"

---

## Samson

### Stats Iniciales
- **HP**: 3
- **Monedas**: 3
- **Ataque**: 1

### Objetos Eternos
- **Bloody Lust**:
  - Tipo: Passive Item
  - Efecto: +1 ATK cada vez que recibes daño este turno

### Habilidad
"Gana +1 ATK permanente cada vez que recibe daño (máximo +3 por turno)"

---

## Azazel

### Stats Iniciales
- **HP**: 1
- **Monedas**: 3
- **Ataque**: 2

### Objetos Eternos
- **Flight**:
  - Tipo: Passive Item
  - Efecto: Inmune a trampas

### Habilidad
"Empieza con 2 ATK y Flight, pero solo 1 HP"

---

## Lazarus

### Stats Iniciales
- **HP**: 2
- **Monedas**: 3
- **Ataque**: 1

### Objetos Eternos
- **Lazarus' Rags**:
  - Tipo: Passive Item
  - Efecto: Al morir, revive con 1 HP y +1 ATK

### Habilidad
"La primera vez que muere, revive automáticamente"

---

## Eden

### Stats Iniciales
- **HP**: Random 1-3
- **Monedas**: Random 0-6
- **Ataque**: Random 0-2

### Objetos Eternos
- 2 ítems aleatorios del mazo de Treasure

### Habilidad
"Stats y objetos iniciales completamente aleatorios cada partida"

---

## The Lost

### Stats Iniciales
- **HP**: 1
- **Monedas**: 3
- **Ataque**: 1

### Objetos Eternos
- **Holy Mantle**:
  - Tipo: Passive Item
  - Efecto: Niega el primer daño que recibes cada turno
- **Flight**:
  - Tipo: Passive Item
  - Efecto: Inmune a trampas

### Habilidad
"Solo puede tener 1 HP. Inmune al primer daño cada turno"

---

## Lilith

### Stats Iniciales
- **HP**: 2
- **Monedas**: 3
- **Ataque**: 1

### Objetos Eternos
- **Box of Friends**:
  - Tipo: Active Item
  - Efecto: Copia un ítem activo que tengas
- **Incubus**:
  - Tipo: Passive Item
  - Efecto: +1 ATK

### Habilidad
"Empieza con Box of Friends e Incubus"

---

## Keeper

### Stats Iniciales
- **HP**: 2
- **Monedas**: 2
- **Ataque**: 2

### Objetos Eternos
- **Wooden Nickel**:
  - Tipo: Active Item
  - Efecto: 50% de ganar 1 moneda
- **Store Credit**:
  - Tipo: Passive Item
  - Efecto: La primera compra cada turno es gratis

### Habilidad
"HP máximo es 2 (monedas). Cuesta 1 moneda curarse"

---

## Apollyon

### Stats Iniciales
- **HP**: 2
- **Monedas**: 3
- **Ataque**: 1

### Objetos Eternos
- **Void**:
  - Tipo: Active Item
  - Efecto: Destruye un ítem para absorber su efecto

### Habilidad
"Puede sacrificar ítems con Void para efectos poderosos"

---

## Configuración en Unity

1. **Crear el ScriptableObject**:
   - Click derecho en Project
   - Create > Four Souls > Character Data
   - Nombrar apropiadamente (ej: "CharacterData_Isaac")

2. **Configurar en Inspector**:
   - Character Type: Seleccionar del enum
   - Character Name: Nombre visible
   - Character Card Front: Sprite revelado
   - Character Card Back: Sprite dorso genérico
   - Starting Health/Coins/Attack: Según tabla
   - Eternal Items: Arrastrar CardDataSO de los ítems
   - Ability Description: Copiar de arriba

3. **Sprites Requeridos**:
   - Crear carpeta `Assets/Resources/Characters/`
   - Subir imágenes de cartas de personaje
   - Formato recomendado: PNG, 512x716 píxeles
   - Nombrar: `Isaac_Front.png`, `Isaac_Back.png`, etc.

4. **Agregar a CharacterSelectionUI**:
   - Abrir CharacterSelectionUI en Inspector
   - En "Available Characters" aumentar Size
   - Arrastrar cada CharacterDataSO creado

## Orden Recomendado de Implementación

1. ✅ Crear CharacterDataSO para Isaac (más simple)
2. ✅ Crear sprites placeholder si no tienes los finales
3. ✅ Testear selección con 1 personaje
4. ✅ Crear 2-3 personajes más para testear selección
5. ✅ Crear todos los personajes restantes
6. ✅ Crear los CardDataSO para objetos eternos
7. ✅ Vincular objetos eternos en cada CharacterDataSO
8. ✅ Testear que stats se apliquen correctamente
9. ✅ Implementar habilidades especiales de personajes
