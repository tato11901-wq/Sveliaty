En este archivo se va a realizar una documentación de las diferentes clases del proyecto
y la utilidad que tiene cada una, de forma que, facilite la comprensión de la función de cada
apartado del proyecto.

1. BasicEnums.cs
En este archivo se administrarán los Enums core del juego. Actualmente se dispone de:

AffinityType: Define las afinidades que pueden tener los ataques, así como la afinidad a la que puede ser débil un enemigo.

EnemyTiers: Define los distintos tiers que puede tener un enemigo. Originalmente se tienen Tier 1, 2 y 3; sin embargo, mantener los datos de esta forma permite añadir otro tier sin romper los datos ya existentes.

2. EnemyTierData.cs
Este script funciona principalmente como una forma de crear enemigos por tiers.
Lo que permite este script es la creación de un tier de un enemigo a través del inspector.

Este script recibe los siguientes datos:

EnemyTiers: El tier del enemigo.

Sprite: El sprite correspondiente a dicho tier.

healthThreshold: La vida del enemigo en ese tier.

diceCount: La cantidad de dados por tirada para ese tier.

maximumDiceThrow: La cantidad de intentos que tiene el jugador para derrotarlo.

failureDamage: El daño que hace el enemigo si no es derrotado por el jugador.

Para crear un tier de un enemigo a través de EnemyTierData, basta con ir a la carpeta Enemies en Scriptable Objects, crear la carpeta correspondiente a dicho enemigo y ahí dar clic derecho → Create → Enemy Tier Data. Automáticamente se creará el objeto para ser modificado desde el inspector.

Ahora, esto sirve para crear un tier de un enemigo. El enemigo en sí se crea desde el siguiente script:

3. EnemyData.cs
Lo que permite EnemyData es, al igual que EnemyTierData, crear un enemigo desde el inspector.
La diferencia es que EnemyData engloba toda la información de un enemigo, guardando los siguientes datos:

id: Identificador único para cada enemigo.

displayName: Nombre que se le muestra al usuario en pantalla, independiente del tier.

AffinityType: Debilidad del enemigo.

EnemyTierData[]: Arreglo que guarda la información de cada uno de los tiers del enemigo.

4. EnemyInstance.cs

Como su nombre lo indica, permite instanciar un enemigo.