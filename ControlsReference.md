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
