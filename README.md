# Juego de billar VR para Meta Quest 3

Prototipo de billar en Unity **6000.0.82f1**, con OpenXR, Meta XR All-in-One SDK **207.0.1** y Meta XR Simulator.

## Abrir el proyecto

1. Clonar el repositorio y añadir su carpeta a Unity Hub.
2. Abrir con Unity 6000.0.82f1 y esperar a que Package Manager restaure los paquetes declarados en `Packages/manifest.json`.
3. Abrir `Assets/Scenes/BilliardsPrototype.unity`.
4. Para pruebas sin visor, instalar y activar Meta XR Simulator desde las herramientas de Meta y ejecutar Play.

Los SDK externos se restauran mediante Package Manager; no se distribuye la caché `Library`. Para compilar a Quest se necesita el módulo Android de Unity y validar la configuración XR de la plataforma.

## Dificultad inicial

Al iniciar aparece un menú con Fácil (troneras de 20 cm), Normal (16 cm) y Difícil (13 cm). Fácil está seleccionada por defecto. En el simulador se elige con Y/H y se confirma con T; en Game de Unity se puede usar 1, 2 o 3. El tamaño cambia también en la geometría y los colliders, manteniendo el paño plano.

## Controles

### Meta XR Simulator

| Acción | Teclas |
|---|---|
| Alternar movimiento / apuntado | B, equivalente a X izquierdo virtual |
| Joystick de desplazamiento en movimiento | Y/H adelante/atrás y G/J izquierda/derecha |
| Apuntar en modo apuntado | G/J horizontal, Y/H elevación |
| Cargar y golpear en la ayuda de práctica | Mantener T y soltar T |

La ayuda de práctica debe estar activa. U da prioridad al agarre por manos simuladas. Los atajos pueden variar si se reasignan en Input bindings del simulador.

### Game de Unity con foco

X alterna los modos. J/L apuntan horizontalmente e I/K cambian la elevación. Mantener y soltar Espacio ejecuta el golpe. Retroceso repone las bolas. F2 alterna ayuda de práctica y manos mientras se apunta.

### Quest 3 físico: implementación pendiente de validar en hardware

Joystick izquierdo para desplazarse y X izquierdo para entrar o salir de apuntado. El taco se equipa en la mano derecha; el golpe usa la retirada y el avance del mando derecho. Grip izquierdo ofrece apoyo opcional. El gatillo no dispara en el visor: el modo de carga con tecla/gatillo es una ayuda exclusiva del Editor.

## Funciones actuales

- Mesa con seis troneras amplias de 20 cm, bandas y colliders.
- Bolas numeradas: 1–8 lisas, 9–15 rayadas y blanca sin número.
- Bolas de 130 g, física ajustable, estela y guía aproximada de trayectoria.
- Apuntado horizontal y vertical, rumbo inicial según la mirada, vista detrás de la blanca y preparación entre tiros.
- Restricción del taco contra la mesa y aislamiento de las bolas respecto a manos, cuerpo y contacto del taco.
- Impulso explícito a la blanca durante un golpe válido.
- Retirada de bolas embocadas; falta de la blanca con recolocación manual en un lugar libre.

## Estructura

```text
Assets/
  Models/
  Materials/
  Prefabs/
  Scenes/
  Scripts/
    Inputs/
    BallPhysics/
    CueInteraction/
    GameRules/
```

`ControlsReference.md` contiene los detalles de controles y las verificaciones realizadas.

## Estado

Validado con pruebas controladas en Unity y pruebas de apuntado y golpe en Meta XR Simulator. La experiencia completa en Quest 3 físico sigue pendiente. La guía de trayectoria es aproximada; todavía no hay reglas completas de bola 8, turnos, victoria, efectos o saltos.
