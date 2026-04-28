# Documento Base Para Presentacion De Movimiento E Interaccion

## Proposito Del Documento

Este documento sirve como base comun para el grupo. La idea es que todos tengan la misma version del proyecto, del enfoque UX y de la forma de evaluarlo, para luego dividir las diapositivas sin contradicciones.

Objetivos de este documento:

- explicar claramente que es el proyecto
- justificar por que encaja en la materia Movimiento e Interaccion
- conectar el proyecto con criterios de UX y usabilidad vistos en clase
- proponer como evaluar esos criterios
- sugerir una division practica de diapositivas para el grupo

---

## Resumen Ejecutivo Del Proyecto

**Nombre del proyecto:** Slot Car Racing AR  
**Tipo de proyecto:** juego movil multijugador local en realidad aumentada  
**Plataforma objetivo:** Android  
**Motor:** Unity 6.3 LTS  
**Jugadores:** 2  
**Modalidad:** local, en la misma mesa, usando hotspot o Wi-Fi comun

### Idea Central

Slot Car Racing AR convierte una mesa real en una pista de carreras miniatura visible en dos telefonos al mismo tiempo. Ambos jugadores apuntan sus celulares a la misma superficie, ven una pista AR compartida y compiten en una carrera corta usando un solo boton de aceleracion.

### Propuesta De Valor

El valor del proyecto no esta en tener muchas funciones, sino en hacer bien una experiencia breve y clara:

- transformar una mesa comun en un espacio de juego compartido
- generar asombro inicial con AR
- ofrecer una competencia local rapida y entendible
- demostrar una experiencia de movimiento e interaccion medible

### Pitch Corto Para La Exposicion

Slot Car Racing AR es un juego de carreras en realidad aumentada para Android donde dos personas convierten una mesa real en una pista compartida. Cada una usa su telefono para ver el mismo circuito, controlar un carro con un solo boton y competir en carreras cortas centradas en timing, claridad visual y experiencia social local.

---

## Por Que Este Proyecto Encaja En Movimiento E Interaccion

El proyecto responde directamente al enfoque de la materia porque integra cuatro elementos clave:

1. **Movimiento fisico del usuario:** el jugador debe orientar y mover el telefono hacia una mesa real para detectar y mantener la referencia visual.
2. **Interaccion mediada por camara:** la camara no es adorno, es el canal principal para construir el espacio de juego.
3. **Relacion entre espacio fisico y sistema digital:** la pista virtual depende de una superficie real y de un marcador fisico compartido.
4. **Interaccion social presencial:** dos usuarios validan la misma experiencia al mismo tiempo y compiten sobre la misma mesa.

En otras palabras, no es solo un videojuego. Es una experiencia donde el cuerpo, el dispositivo, la mesa, la camara y la interfaz trabajan juntos para crear una interaccion jugable.

---

## Descripcion Del Proyecto

### Objetivo General

Desarrollar una experiencia AR multijugador local para Android en la que dos jugadores compartan una pista de carreras tipo slot car sobre una mesa real usando sus telefonos.

### Objetivos Especificos

- lograr un anclaje estable de una pista 3D sobre una mesa real
- permitir que dos jugadores vean y jueguen sobre la misma pista
- ofrecer una experiencia facil de aprender en la primera sesion
- mantener un flujo corto: entrar, confirmar, correr y repetir
- demostrar que la experiencia puede evaluarse desde criterios de UX y usabilidad

### Alcance Del MVP

Incluye:

- Android como unica plataforma
- Unity como motor
- 2 jugadores obligatorios
- 1 pista predefinida
- 1 boton de aceleracion por jugador
- partida local por hotspot o Wi-Fi comun
- contador de vueltas, resultado y revancha

No incluye:

- iOS
- juego online
- varias pistas en la primera entrega
- freno, turbo, drift o power-ups
- personalizacion compleja de carros

---

## Usuarios Y Contexto De Uso

### Usuarios Objetivo

- estudiantes universitarios y jovenes familiarizados con juegos moviles casuales
- usuarios con baja tolerancia a configuraciones largas
- personas interesadas en experiencias sociales presenciales mas que en simulacion compleja

### Contexto De Uso

- aula, laboratorio, casa o espacio interior con mesa disponible
- sesiones de 3 a 10 minutos
- iluminacion moderada o buena
- dos personas alrededor de la misma superficie

### Necesidades Del Usuario

- entender rapido como entrar a la experiencia
- saber si tracking y conexion estan funcionando
- ver claramente la pista y el HUD
- sentir que el resultado depende de habilidad y no de falla tecnica

---

## Flujo De La Experiencia

El flujo principal del proyecto es corto y legible:

1. un jugador crea la sesion y el otro se une
2. ambos apuntan al marcador o a la superficie compartida
3. el sistema confirma que la pista esta lista
4. ambos jugadores validan que pueden competir
5. inicia la cuenta regresiva
6. se corre la partida usando un solo boton de aceleracion
7. se muestra el resultado
8. se ofrece revancha sin rehacer todo el setup si la referencia sigue estable

### Regla Central De Juego

La mecanica se resume en una frase:

**Mantener para acelerar. Soltar en curvas.**

Eso hace que el sistema sea facil de aprender, pero permite que la diferencia de timing genere habilidad real.

---

## Enfoque UX Del Proyecto

Desde la especificacion UX del proyecto, la experiencia debe sentirse como un ritual social breve de mesa, no como una app tecnica. La secuencia emocional buscada es:

- asombro inicial al ver aparecer la pista sobre la mesa
- confianza compartida cuando ambos validan la misma referencia
- tension competitiva durante la carrera
- deseo inmediato de revancha al finalizar

Principios UX que guian el proyecto:

- claridad antes que espectacularidad
- el sistema siempre debe mostrar su estado
- la confianza compartida importa mas que la precision perfecta
- una sola accion debe producir una consecuencia clara
- la revancha debe ser mas rapida que la configuracion inicial
- cuando algo falla, el sistema debe explicarlo y proponer una accion concreta

---

## Criterios De UX Y Usabilidad Del Curso

En el PDF de clase aparece la idea de que la usabilidad busca sistemas:

- eficientes
- efectivos
- seguros
- utiles
- faciles de aprender
- faciles de recordar

Tambien se conecta con la idea clasica de usabilidad como calidad de uso: eficacia, eficiencia y satisfaccion. Para la presentacion conviene aterrizar esos conceptos al proyecto asi:

| Criterio | Que significa en este proyecto | Como se puede evaluar |
| --- | --- | --- |
| Efectivo | Que los jugadores logren completar el flujo principal: unirse, ver la pista, correr y terminar una carrera | porcentaje de usuarios que terminan la tarea completa sin abandonar ni quedar bloqueados |
| Eficiente | Que el sistema permita llegar rapido al juego sin pasos sobrantes ni ayudas excesivas | tiempo total de setup, tiempo para unirse, numero de ayudas del facilitador, numero de intentos para dejar la pista lista |
| Seguro | Que el sistema prevenga errores graves y que, si falla tracking o red, no deje al usuario en estados confusos o irrecuperables | numero de errores criticos, tasa de recuperacion, cantidad de reinicios completos necesarios, claridad del mensaje de error |
| Util | Que realmente resuelva la necesidad propuesta: convertir una mesa real en una carrera AR compartida, entendible y divertida | preguntas post-prueba sobre valor percibido, si el usuario entendio para que sirve la experiencia y si la considera funcional |
| Facil de aprender | Que un usuario nuevo entienda rapido que hacer y como jugar, idealmente en la primera sesion | tiempo hasta la primera carrera completa, cantidad de dudas, necesidad de tutorial adicional, comprension de la regla del boton |
| Facil de recordar | Que, despues de un tiempo, el usuario pueda repetir el flujo sin volver a recibir explicacion completa | segunda prueba despues de una pausa, comparacion entre primer y segundo intento, cantidad de recordatorios necesarios |

### Complemento Recomendado: Satisfaccion

Aunque no aparezca en la lista principal que quieres exponer, vale la pena mencionar satisfaccion porque el material del curso conecta usabilidad con calidad de uso.

En este proyecto, la satisfaccion se puede observar en:

- si el usuario quiere revancha
- si percibe la experiencia como justa
- si siente asombro y control
- si recomienda repetir la experiencia

---

## Propuesta De Evaluacion Del Proyecto

### Que Se Va A Evaluar

Se evaluan dos cosas al mismo tiempo:

1. si el proyecto funciona como experiencia interactiva AR
2. si cumple con criterios concretos de UX y usabilidad

### Tareas De Evaluacion

Las tareas de prueba pueden ser estas:

1. crear o unirse a una partida
2. apuntar correctamente a la mesa y dejar la pista lista
3. entender la regla del boton de aceleracion
4. completar una carrera corta
5. usar revancha o recuperarse ante una falla guiada

### Metricas Practicas Para Mostrar En Clase

- tiempo de setup completo: objetivo menor a 120 segundos
- tiempo de union a la sesion: objetivo menor a 30 segundos
- porcentaje de usuarios que logran completar una carrera
- numero de errores criticos por sesion
- numero de ayudas necesarias para completar el flujo
- tiempo de recuperacion ante tracking perdido o desconexion
- percepcion de claridad, control y justicia en una encuesta corta

### Instrumentos De Evaluacion

Para una prueba academica sencilla, sirven estos instrumentos:

- **observacion directa:** mirar donde se detiene el usuario, donde duda y que parte del flujo lo confunde
- **cronometro:** medir tiempos de setup, ingreso, carrera y revancha
- **checklist de tareas:** marcar si logro o no cada parte del flujo
- **registro de errores:** anotar fallas de tracking, red, confusion o reinicios
- **mini encuesta posterior:** 5 o 6 preguntas cortas sobre claridad, control, utilidad y deseo de repetir
- **preguntas abiertas:** que fue facil, que fue dificil, que mejorarian

### Ejemplo De Preguntas Para Encuesta Post-Prueba

- Entendi rapidamente que tenia que fhacer para jugar.
- La experiencia me parecio clara y ordenada.
- Senti que la pista estaba compartida de forma confiable.
- El control con un solo boton fue facil de entender.
- Si algo fallaba, el sistema explicaba bien que hacer.
- Me dieron ganas de repetir la carrera.

Escala sugerida: 1 a 5.

---

## Riesgos O Puntos Criticos Que Conviene Mencionar En La Exposicion

- tracking inestable por luz o angulo de camara
- desalineacion percibida entre los dos jugadores
- inestabilidad de la red local
- degradacion de rendimiento por calor del dispositivo
- dificultad para mantener la pista clara y el HUD simple al mismo tiempo

Estos riesgos no debilitan la presentacion. Al contrario: muestran que el proyecto entiende sus limites y propone evaluarlos.

---

## Propuesta De Estructura De Diapositivas

Una presentacion clara podria tener 10 diapositivas:

| Diapositiva | Tema | Mensaje principal |
| --- | --- | --- |
| 1 | Portada | Nombre del proyecto, integrantes y frase corta del concepto |
| 2 | Problema y oportunidad | Como una mesa real puede convertirse en una experiencia de juego compartida |
| 3 | Descripcion del proyecto | Que es Slot Car Racing AR y como funciona en terminos generales |
| 4 | Usuarios y contexto | Para quien es y en que contexto se usa |
| 5 | Flujo de interaccion | Crear, unirse, escanear, correr, revancha |
| 6 | UX del proyecto | Principios de diseno y experiencia buscada |
| 7 | Criterios UX del curso | Efectivo, eficiente, seguro, util, facil de aprender y facil de recordar |
| 8 | Evaluacion | Tareas, metricas e instrumentos de prueba |
| 9 | Tecnologia y viabilidad | Android, Unity, AR, multijugador local y alcance MVP |
| 10 | Cierre | Valor academico del proyecto, riesgos y posible demo |

---

## Division Sugerida Para El Grupo

Como no se exactamente cuantas personas son, la division se propone por bloques tematicos.

### Si Son 4 Personas

- **Persona 1:** diapositivas 1 y 2
- **Persona 2:** diapositivas 3 y 4
- **Persona 3:** diapositivas 5, 6 y 7
- **Persona 4:** diapositivas 8, 9 y 10

### Si Son 5 Personas

- **Persona 1:** diapositivas 1 y 2
- **Persona 2:** diapositivas 3 y 4
- **Persona 3:** diapositivas 5 y 6
- **Persona 4:** diapositivas 7 y 8
- **Persona 5:** diapositivas 9 y 10

### Si Son 6 Personas

- **Persona 1:** diapositiva 1
- **Persona 2:** diapositivas 2 y 3
- **Persona 3:** diapositivas 4 y 5
- **Persona 4:** diapositivas 6 y 7
- **Persona 5:** diapositivas 8 y 9
- **Persona 6:** diapositiva 10 y cierre general

### Recomendacion De Reparto Por Perfil

- quien mejor explica contexto, toma problema y justificacion
- quien mejor domina el juego, toma concepto y flujo
- quien mejor domina UX, toma criterios y evaluacion
- quien mejor domina tecnologia, toma arquitectura minima y viabilidad
- quien mejor expone, toma cierre y demo

---

## Mensajes Clave Que No Deben Faltar En La Exposicion

- el proyecto no intenta ser un juego complejo, sino una experiencia AR clara y medible
- la fuerza del proyecto esta en compartir una misma mesa y una misma pista entre dos personas
- la UX no se presenta como teoria suelta: se conecta con decisiones concretas del diseno
- el proyecto puede evaluarse con tareas, tiempos, errores y percepcion del usuario
- el valor academico esta en unir movimiento, interaccion, realidad aumentada y usabilidad en una experiencia funcional

---

## Material Fuente Que Respalda Este Documento

- GDD del proyecto
- especificacion UX del proyecto
- arquitectura del proyecto
- PDF de Experiencia de Usuario I

Este documento ya esta pensado como base para repartir trabajo, no como version final de las diapositivas. La recomendacion es usarlo para asignar temas hoy y luego convertir cada bloque en 1 o 2 slides visuales.