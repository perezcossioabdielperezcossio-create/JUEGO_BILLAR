# Controles — Billar VR para Meta Quest 3

## Dos modos de juego

El juego inicia en modo SENTADO. B del mando derecho alterna entre SENTADO y MOVIMIENTO FISICO. El visor conserva su seguimiento en ambos modos.

| Acción | Control |
|---|---|
| Desplazarse alrededor de la mesa | Joystick izquierdo, fuera del apuntado |
| Entrar o salir del apuntado | X del mando izquierdo |
| Cambiar modo sentado / movimiento físico | B del mando derecho |
| Mirar sin mover la cabeza, en ambos modos | Joystick derecho: horizontal y vertical, fuera del apuntado |
| Orientar y elevar el taco, en modo sentado y apuntado | Joystick derecho |
| Cargar fuerza, en modo sentado y apuntado | Mantener el gatillo de índice derecho |
| Golpear con la fuerza cargada | Soltar el gatillo derecho |
| Golpear en modo de movimiento físico | Retirar y avanzar el mando derecho contra la blanca |
| Apoyo opcional de dos manos, en modo físico | Grip izquierdo |

La carga máxima se alcanza aproximadamente en 1.22 segundos. La fuerza deja de aumentar al llegar al máximo. Durante la carga se bloquea la orientación del taco para conservar el tiro. El golpe pasa por el mismo detector de punta contra la blanca que utiliza el modo físico; mover el taco al apuntar no golpea las bolas.

Al apuntar se bloquea el desplazamiento y la cámara se coloca detrás de la blanca. En modo sentado la cámara acompaña los ajustes de dirección y elevación. Hay que esperar a que se detengan las bolas y termine la alineación antes de cargar otro tiro.

Al salir del apuntado, cambiar de modo, abrir un bloqueo de controles o desconectar el mando derecho se cancela una carga pendiente. Reconectarlo no debe disparar por sí solo. El modo sentado lee botones y joystick aunque no haya seguimiento de la posición del mando; el golpe físico sí requiere seguimiento. Se usa OpenXR con lectura de respaldo de los mandos Touch del SDK Meta.

Al detectar una pose válida del visor, se compensa el desplazamiento del área física y se alinea al jugador con su punto de inicio dentro del cuarto. La gravedad espera esa calibración. El giro vertical ocurre alrededor de los ojos, conservando su posición. Un cambio del origen de seguimiento vuelve a calibrar la vista. La altura sentada se compensa a 1.6 m; se conserva el seguimiento real de la cabeza.

## Menú y falta de blanca

- Dificultad: joystick arriba/abajo para elegir y gatillo para confirmar. Fácil: troneras de 20 cm; normal: 16 cm; difícil: 13 cm.
- Blanca embocada: esperar a que se detengan las bolas, apuntar a un lugar libre del paño y pulsar el gatillo derecho para colocarla. Esa pulsación no inicia un golpe.

## Pruebas sin visor

En Meta XR Simulator se usan los estados OpenXR de los mismos mandos. Revisar sus asignaciones de teclado en la versión instalada; no confundir B del teclado con B del mando: B del teclado estaba asignada a A/X virtual. T representa el gatillo virtual; G/J y Y/H representan los ejes del joystick en la configuración utilizada.

Con foco en Game de Unity:

- F3: alternar sentado / movimiento físico.
- X: alternar desplazamiento / apuntado.
- J/L: horizontal; I/K: vertical. En sentado sirven para mirar o apuntar según el estado.
- Mantener Espacio y soltar: cargar y golpear en el control asistido.
- F2: alternar ayuda asistida en el editor durante el apuntado.
- Retroceso: reponer bolas durante el control asistido.
- 1/2/3: elegir dificultad; Enter: confirmar menú o recolocación.

## Organización de scripts

- `Assets/Scripts/Entradas`: lectura de mandos, movimiento, modos y cámara.
- `Assets/Scripts/FisicaBolas`: cuerpos de bolas, trayectoria y aislamiento de manos.
- `Assets/Scripts/InteraccionTaco`: agarre, restricciones, control asistido e impacto.
- `Assets/Scripts/ReglasJuego`: dificultad, troneras y falta/recolocación.

Se conservaron los nombres de clases y sus espacios de nombres internos para mantener la compatibilidad. Se movieron las carpetas con AssetDatabase para conservar los identificadores y referencias de escenas y prefabs. DesktopCuePractice ahora contiene el control asistido compartido con Android; únicamente el teclado y los ajustes exclusivos del editor están bajo UNITY_EDITOR.

## Alcance

La guía de trayectoria es aproximada. Los golpes aplican impulso horizontal central; la elevación permite orientar el taco y librar bandas, pero no introduce saltos ni efectos. La validación de comodidad, seguimiento y rendimiento en Meta Quest 3 físico requiere probar la nueva versión en el visor. El APK anterior no incluye estos controles.
