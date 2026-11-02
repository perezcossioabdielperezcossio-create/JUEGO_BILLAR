# Ambiente de bar — modelos editables

La escena BilliardsPrototype tiene el cuarto y el conjunto BarEnvironment. La mesa y sus controles mantienen sus objetos propios.

## Modelos y prefabs
- Assets/Models/Bar: 14 modelos base editables.
- Assets/Prefabs/Bar: prefabs individuales que usan esos modelos.
- Assets/Prefabs/BarEnvironment.prefab: conjunto completo para reutilizar en otra escena.
- Assets/Materials: materiales BarWalnut, BarDarkWood, BarBrass, BarLeather, BarLampGreen y otros colores de las botellas.

Modelos: Ceiling, BarCounter, BottleShelf, BarStool, PendantLamp, ClubSign, EightBallPoster, GoodTimesPoster, CueWallRack, LoungeBench, DrinkTable, DoorPanel, RoomFinishes y BarLighting.

## Hacer cambios pequeños en Unity
1. Para mover un mueble, selecciona su instancia dentro de BarEnvironment en Hierarchy y modifica Transform.
2. Para cambiar todas las copias de un modelo, abre su archivo en Models/Bar con doble clic, edítalo en Prefab Mode y guarda.
3. Para modificar solo una copia, cambia la instancia de la escena y conserva sus overrides. Aplicar los overrides al prefab hace que el cambio se reutilice.
4. Para cambiar colores, selecciona el material correspondiente en Materials.
5. Las botellas son piezas editables dentro de BottleShelfModel. Los textos están dentro de los modelos de letreros y cuadros.

## Luces y techo
BarLightingModel contiene tres luces reales: PoolTableSpot (principal sobre la mesa, intensidad 6 y sombras), BackBarGlow (barra) y LoungeGlow (zona de banco). Solo la luz principal usa sombras. Las tres lámparas visibles se editan en PendantLampModel; sus difusores son materiales luminosos decorativos.

El techo comienza a 3.2 m sobre el suelo, tiene un collider y vigas decorativas. Las luces ambientales cálidas están guardadas en la escena. El panel de puerta es decorativo; no abre la pared.

Verificación realizada: techo bloquea un rayo desde abajo a y=3.2, tres luces activas, render de vista previa revisado y textos corregidos para caber en sus marcos. La decoración utiliza geometría simple y modelos independientes. El rendimiento final en Quest 3 físico sigue pendiente de medir.

## Corrección de apoyos y colisiones
Los modelos fuente incluyen colliders ajustados a sus mallas, excepto los textos. El banco tiene cuatro patas apoyadas en y=0, los estantes y el reposapiés tienen soportes y los cables de las lámparas alcanzan el techo. Se conservaron los prefabs anidados para seguir editando cada mueble por separado. Los colliders sólidos bloquean tanto al jugador como al taco; las bolas siguen aisladas de manos y cuerpo por la lógica de billar.
