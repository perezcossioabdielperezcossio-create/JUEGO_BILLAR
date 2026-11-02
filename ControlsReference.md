# Controles vigentes — Billar Quest 3

## Quest 3 físico
El juego comienza en MOVIMIENTO.
- Joystick izquierdo: desplazarse con referencia a la dirección horizontal del visor, 1.2 m/s; cápsula de colisión para evitar cruzar la mesa.
- X del mando izquierdo: alternar MOVIMIENTO / APUNTADO. Se cambia una vez por pulsación.
- En APUNTADO, el joystick no desplaza al jugador y el taco se equipa automáticamente en la mano derecha. No es necesario mantener grip derecho.
- Mover y girar el mando derecho: mover y orientar el taco.
- Retirar y avanzar físicamente el taco contra la blanca: golpear. La fuerza depende de la velocidad de la punta, limitada a 3 m/s.
- Grip izquierdo: apoyo opcional para orientar el taco con dos manos, entre 0.15 y 1.1 m de separación.
- X de nuevo: guardar el taco y volver a moverse.
- Perder seguimiento derecho libera el taco; el seguimiento restablecido permite recuperarlo en apuntado.
- Gatillo derecho: no dispara en la compilación para Quest.

## Meta XR Simulator, sin visor
Requiere ambos inputs configurados como Controller y runtime activo.
- B del teclado equivale a A/X virtual; X izquierdo cambia el modo.
- MOVIMIENTO: Y/H representan adelante/atrás del stick; G/J, izquierda/derecha.
- APUNTADO: se activa la ayuda de práctica del Editor. G/J orientan el taco, mantener T carga y soltar T golpea. La ayuda mueve el taco y usa CueStrikeController.
- B otra vez vuelve a MOVIMIENTO.
- No mantener U al usar la ayuda: el grip da prioridad al control mediante manos simuladas.
- WASD y R/F son movimiento del visor simulado; no prueban la locomoción del joystick del juego.

## Game de Unity con foco
- X del teclado: alternar modos.
- En APUNTADO: J/L orientan, mantener y soltar Espacio golpea; Retroceso repone las bolas.
- F2 alterna ayuda de práctica y control por manos, únicamente en APUNTADO.

## Validación y límites
- SDK Meta XR All-in-One 207.0.1, OpenXR y scripts compilan.
- Simulador: ambos Touch Plus detectados, seguimiento, agarres y gatillos recibidos. El usuario confirmó apuntado y golpe desde la ventana del simulador antes de incorporar locomoción.
- Pruebas controladas del nuevo modo: joystick mueve; X equipa; apuntado bloquea desplazamiento; mantener X no repite; segunda pulsación devuelve movimiento. Todas aprobadas.
- Golpe: cruce rápido y punta inicialmente solapada detectan un impacto; límite visual de penetración aprobado.
- Quest 3 físico todavía no validado; falta prueba integrada de locomoción y golpe en hardware.
- Física provisional: sin efectos, troneras ni reglas. La colisión se resuelve en la punta contra la blanca, no en todo el cuerpo del taco.

## Realismo, guía y vista de apuntado
- CueTableConstraint comprueba la cápsula completa del taco contra tablero, bandas, bastidor y patas. Muestrea traslación y rotación para evitar atravesar la mesa entre frames; conserva la última pose libre.
- Prueba controlada: mover el taco de y=1.4 a y=0.4 quedó bloqueado en y=0.803 sin penetración.
- La blanca lleva una estela breve durante el movimiento. Al apuntar y estar quieta, aparece una guía plana de dirección y un rebote estimado; se corta al encontrar otra bola.
- Simulador: G/J giran en horizontal y Y/H ajustan elevación. Game de Unity: J/L e I/K. Elevación entre 0 y 35 grados; inicial 8 grados para elevar la culata sobre las bandas.
- En Quest, la orientación 3D sigue al mando derecho y al apoyo izquierdo cuando se usa.
- Al entrar en apuntado se desplaza suavemente TrackingSpace hasta una altura objetivo de ojos de 0.96 m, unos 18 cm sobre el tablero. Al salir se recupera el offset original. El visor mantiene su seguimiento y orientación; la cápsula de locomoción no se desplaza mientras se apunta.
- Pruebas: horizontal y elevación cambian, altura de apuntado 0.96 m observada, guía generada con 3 puntos y estela presente. Pendiente validar sensación, visibilidad y comodidad en Quest físico.
- La guía es aproximada: no modela efectos, fricción ni pérdida de energía en el rebote. El golpe sigue transfiriendo impulso horizontal central; elevar el taco no habilita todavía saltos ni efectos.

## Protección del apuntado y vista detrás de la blanca
- BallContactIsolation ignora contactos entre las 16 bolas y todos los colliders del jugador/manos y del taco, incluidos los que aparecen después de iniciar. Las bolas mantienen sus colisiones con mesa y entre sí.
- El taco conserva su restricción contra la mesa mediante CueTableConstraint, pero su cuerpo atraviesa las bolas sin empujarlas. Solo la punta durante un golpe válido transfiere impulso a la blanca.
- Práctica del simulador: girar o colocar el taco no permite golpear; solo la fase de avance al soltar T/Espacio activa el detector.
- Quest: se exige una retirada de al menos 2 cm y avance del mando dominante en dirección del taco. El giro de la punta sin desplazamiento del mando no dispara. Durante la transición de cámara los golpes están bloqueados.
- Al entrar en apuntado, la vista se coloca suavemente a 60 cm detrás de la blanca y 15 cm por encima, mirando hacia ella. El desplazamiento y la orientación son un ajuste inicial del origen de seguimiento; el visor conserva sus movimientos. Al salir se restaura el origen original.
- El diagnóstico grande está oculto por defecto (showDetailedReadout lo reactiva para depurar).
- Pruebas controladas aprobadas: 16 bolas excluyen jugador/taco; choques bola-bola y bola-mesa conservados; apuntado no dispara; retirada y avance sí golpean; cámara mira la blanca a distancia 0.618 m; fase de disparo del simulador genera un impacto sin impulso directo a otras bolas. Pendiente prueba física en Quest.

## Regla de alineación entre tiros — práctica del simulador/Editor
- Después de cada recorrido de golpe se vuelve al estado de preparación.
- Las bolas deben estar detenidas durante 0.25 s. El taco se coloca detrás de la posición actual de la blanca y se reinicia el historial de impacto para evitar disparos de reposicionamiento.
- Se verifica que el eje del taco pase a menos de 3 mm del centro de la blanca y que la punta quede detrás de ella.
- Se comprueban tanto la pose de reposo como la retirada máxima contra la mesa. La elevación aumenta automáticamente hasta encontrar una pose legal (hasta 75 grados).
- La restricción de mesa adopta esa nueva pose libre entre tiros y deja de conservar el anclaje del tiro anterior. Durante el golpe sigue evitando atravesar la mesa.
- La cámara de mira se actualiza a la nueva blanca y orientación del taco. La carga se bloquea mientras la vista está en transición o falta preparación.
- Estados visibles: ESPERA A QUE PAREN LAS BOLAS, ALINEANDO CON LA BLANCA y LISTO.
- Pruebas controladas de tres preparaciones consecutivas (centro, banda lateral y banda de fondo): alineadas tras aplicar la restricción, sin penetración y con error de eje inferior a 0.001 mm. Preparación bloqueada con bolas en movimiento.
- Esta recolocación automática pertenece a la ayuda de práctica del Editor/simulador. En Quest físico, el apuntado sigue la mano y el impacto exige contacto válido con la blanca.

## Troneras, bolas numeradas y respuesta ligera
- Mesa con seis aperturas físicas de radio 0.10 m (20 cm de diámetro), cuatro esquinas y dos laterales. Tablero con malla recortada, bandas abiertas, interiores oscuros y aros visuales. Nuevo prefab BilliardsTablePockets.
- PocketTrigger captura bolas solo al entrar en la abertura; una bola de color se hunde y se desactiva. La blanca se repone cuando las otras bolas se detienen y busca un lugar libre cerca de su salida.
- Colores y números basados en la referencia: 1–8 lisas (amarillo, azul, rojo, violeta, naranja, verde, burdeos, negro), 9–15 rayadas con esos siete colores; blanca sin número. La 8 ocupa el centro del triángulo.
- Masa de 0.13 kg, amortiguación lineal 0.07 y angular 0.045. Transferencia del taco 0.98, fricción de bolas 0.06 y del paño 0.14. Son valores iniciales ajustables; la mayor movilidad viene también de reducir pérdidas, no solo de la masa.
- La guía de apuntado se corta al llegar a una tronera y excluye las bolas retiradas. La preparación se bloquea mientras la blanca está embocada.
- Validación: seis rayos verticales pasan por los huecos y el centro del paño sigue sólido; 15 números únicos; dos capturas físicas registradas, retirada de la bola 1 y retorno de la blanca confirmados. Runtime: masa 0.13 kg.
- No se han añadido todavía reglas completas de bola 8, turnos, victoria o puntuación.

## Paño plano, dirección inicial y falta de la blanca
- Medición del tablero: todos los vértices del paño tienen y=0.78 m, rotación de mesa 0/0/0 y gravedad vertical. No hay rampas ni inclinaciones a las troneras.
- El detector de tronera ya no captura una bola al nivel del paño. La captura exige una caída real de al menos 4 mm desde la altura de reposo de la bola sobre el paño y revisa el segmento recorrido; el inicio de la caída es por gravedad.
- Al pulsar X/B para entrar en apuntado se captura el rumbo horizontal actual del visor. El taco y la mira usan ese mismo rumbo. Al moverse y entrar desde otro lado se toma una nueva dirección; no se vuelve al +Z global ni al ángulo del primer tiro.
- Verificación de direcciones: mirar a +90, -90 y 180 grados produjo esos mismos rumbos de taco, con error inferior a 0.00001 grados.
- Embocar la blanca es falta: espera al reposo de otras bolas y entra en BOLA EN MANO, sin regresar automáticamente a la salida.
- Para colocarla, apuntar el mando derecho a un lugar libre del paño. Si el rayo del mando no llega al paño, se usa la dirección de la mirada. Anillo verde: válido; rojo: inválido. Gatillo derecho/T confirma; en Game de Unity también Enter.
- No se permite colocar encima de otra bola, fuera de la mesa ni cerca de una tronera. Después de colocar se prepara un nuevo tiro y se exige una nueva pulsación del gatillo para cargar; la confirmación no debe disparar.
- Pruebas: captura bloqueada a la altura del paño; caída natural de la blanca; espera de colocación; rechazo de bola ocupada y tronera; aceptación de punto libre; retorno a estado jugable. No incluye cambios de turno ni reglas completas de bola 8.

## Menú inicial y dificultad
- Al iniciar Play aparece un menú pequeño con Fácil, Normal y Difícil. Fácil está seleccionada por defecto y conserva las troneras amplias anteriores.
- Fácil: diámetro 20 cm; Normal: 16 cm; Difícil: 13 cm.
- Solo cambia la apertura de las troneras: la geometría recortada del paño, los huecos entre bandas, los aros/interiores y los detectores de captura. No cambia masa ni fuerza de golpe.
- Quest: joystick arriba/abajo para seleccionar y gatillo para confirmar.
- Meta XR Simulator: Y/H para elegir y T para confirmar. Confirmar T sin cambiar la selección inicia Fácil.
- Game de Unity: 1, 2 o 3 eligen e inician; Enter confirma la opción resaltada. También admite clic sobre los botones del menú con Game enfocado.
- Desplazamiento y apuntado permanecen bloqueados hasta confirmar dificultad.
- Pruebas en Play: selección inicial 0/Fácil y bloqueo activo; los seis detectores coinciden con cada radio; rayos al paño distinguen los tres huecos reales; superficie plana en las tres opciones.

## Cuarto de billar
- Escena BilliardsPrototype: cuarto interior de 7 x 8.5 m, con cuatro paredes de 3.2 m de altura y suelo horizontal a y=0.
- Paredes con BoxCollider; suelo con BoxCollider. Se conservan espacio de movimiento alrededor de la mesa y altura de paño y=0.78.
- Materiales RoomWall, RoomAccentWall, RoomFloor y RoomBaseboard.
- Prefab compuesto Assets/Prefabs/BilliardsRoom.prefab. Modelos individuales de paredes y suelo en Assets/Models/Room.

## Ambiente de bar
- Techo con vigas y collider, barra con estantes y botellas, tres taburetes, tres lámparas colgantes, banco, mesita, taco decorativo en pared, cuadros, panel de puerta y alfombra.
- 14 modelos base en Models/Bar, prefabs individuales en Prefabs/Bar y conjunto BarEnvironment.prefab.
- Iluminación cálida: tres luces reales; solo la principal sobre la mesa utiliza sombras. Los cambios pequeños se explican en BAR_MODELS_README.md.

## Contención de troneras y colisiones del ambiente
- PocketTrigger comprueba también el segmento recorrido por cada bola en un paso de física. Captura cruces rápidos aunque no se reciba un evento de trigger a tiempo; no captura una bola que todavía está apoyada en el paño.
- Cada dificultad genera paredes interiores y guardas en la cara exterior de las seis troneras. Se eliminan las contenciones anteriores al cambiar tamaño. En Fácil hay 120 piezas de contención activas, sin duplicados.
- La animación de embocado dirige la bola hacia el centro interior de la tronera antes de retirarla. La blanca conserva la falta y recolocación manual.
- Todos los elementos de malla sólidos del bar y el cuarto tienen collider; los textos son visuales. Los cambios se guardaron en 19 prefabs fuente de Models/Bar y Models/Room y en el prefab compuesto del cuarto.
- Se añadieron cuatro patas al banco, soportes al estante y al reposapiés de la barra y se extendió la suspensión de las lámparas hasta el techo.
- CueTableConstraint también considera los sólidos del cuarto y el mobiliario; sigue excluyendo jugador y bolas para conservar el aislamiento del apuntado.
- Pruebas: 18 cruces de captura (6 troneras x 3 dificultades), entradas físicas a 3 m/s por lateral y esquina, cero bolas sueltas bajo la mesa, bloqueo real de CharacterController ante mesita y pared, banco apoyado en suelo y suspensión de lámparas en techo. Compilación correcta.
