---
document_type: gdd
project_name: Slot Car Racing AR
title: Slot Car Racing AR
status: draft-v1
language: es
author: Jpinzon
date: 2026-04-14
platform: Android
engine: Unity
scope: MVP cerrado
---

# Slot Car Racing AR - Game Design Document

## 1. Resumen Ejecutivo

### 1.1 Concepto Central

Slot Car Racing AR es un juego movil de carreras en realidad aumentada para Android en el que dos jugadores convierten una mesa real en una pista de slot cars compartida. Cada jugador usa la camara de su telefono para detectar un marcador fisico sobre la mesa, ver la misma pista 3D anclada al entorno y competir cara a cara en carreras cortas basadas en aceleracion y timing.

### 1.2 Propuesta de Valor

La experiencia no busca competir por profundidad de simulacion ni por cantidad de contenido. Su valor esta en la interaccion compartida:

- transformar un espacio fisico comun en una pista visible para todos
- generar asombro inmediato al ver aparecer el circuito sobre la mesa
- ofrecer competencia local rapida, clara y facil de aprender
- demostrar una experiencia de movimiento e interaccion centrada en AR y usabilidad

### 1.3 Vision del Juego

Dos jugadores comparten una mesa real, ven la misma pista AR desde sus propios telefonos y compiten en una carrera tipo slot car con un solo boton de aceleracion, donde ganar depende de leer las curvas y controlar el timing mejor que el oponente.

### 1.4 Estado del Proyecto

Este documento define el MVP objetivo para desarrollo entre el 21 de abril y el 26 de mayo de 2026, con entrega funcional en Android y enfoque en experiencia terminada, no en prototipo conceptual.

## 2. Descripcion y Justificacion del Proyecto

### 2.1 Descripcion

El proyecto consiste en una experiencia ludica multijugador local en AR. Sobre una mesa con un marcador fisico grande y de alto contraste, el sistema detecta una referencia visual estable y coloca una pista 3D predefinida. Dos jugadores conectados a la misma red local o hotspot observan la misma pista desde sus dispositivos y controlan un carro cada uno mediante un unico boton de aceleracion.

### 2.2 Justificacion

Este proyecto responde de forma directa al enfoque de la materia Movimiento e Interaccion porque integra:

- espacio fisico real como superficie de juego
- interaccion mediada por camara y movimiento del dispositivo
- experiencia compartida entre multiples usuarios
- decisiones de UX orientadas a usabilidad, claridad, learnability y satisfaccion

La decision de usar una estructura tipo slot car, en lugar de conduccion libre, reduce complejidad tecnica y centra la experiencia en una mecanica entendible y medible. Eso permite que el valor principal del proyecto sea la calidad de la interaccion AR y la percepcion de juego compartido.

## 3. Objetivos, Alcance e Impacto Deseado

### 3.1 Objetivo General

Desarrollar un juego movil AR multijugador local y funcional para Android, en el que dos jugadores compartan una pista de carreras tipo slot car sobre una mesa real usando sus telefonos.

### 3.2 Objetivos Especificos

- lograr anclaje estable de una pista 3D sobre un marcador fisico en mesa
- permitir que dos jugadores vean la misma pista y corran una partida completa
- ofrecer una experiencia clara de onboarding, carrera, resultados y revancha
- demostrar una interaccion facil de aprender y atractiva en una primera sesion
- mantener el proyecto dentro de un alcance implementable en el tiempo disponible

### 3.3 Alcance del MVP

Incluye:

- Android como unica plataforma objetivo
- Unity como motor de desarrollo
- 2 jugadores como requisito obligatorio
- 1 pista predefinida y cerrada
- 1 marcador fisico compartido
- 1 boton de aceleracion por jugador
- partida local sobre hotspot o Wi-Fi comun
- contador de vueltas, resultado y revancha

No incluye:

- iPhone o iOS
- juego online
- personalizacion de carros
- mas de una pista en la primera entrega
- procedural generation
- freno, turbo, drift o power-ups

### 3.4 Impacto Deseado

El juego debe provocar tres resultados principales:

- sorpresa al transformar una mesa comun en un circuito AR
- comprension rapida de la dinamica sin tutorial largo
- deseo inmediato de repetir la carrera por rivalidad y mejora de timing

## 4. Usuario Objetivo y Contexto de Uso

### 4.1 Usuario Objetivo

Usuarios principales:

- estudiantes universitarios o jovenes familiarizados con juegos moviles casuales
- personas capaces de sostener el telefono apuntando a una superficie por partidas cortas
- jugadores que valoran experiencias sociales presenciales mas que simulacion compleja

### 4.2 Contexto de Uso

El juego esta pensado para:

- aula, laboratorio, casa o espacio interior con una mesa disponible
- sesiones de 3 a 10 minutos
- entornos con iluminacion moderada o buena
- dos jugadores alrededor de la misma superficie

### 4.3 Necesidades del Usuario

- entender rapidamente como iniciar la experiencia
- saber si el tracking y la conexion estan funcionando
- ver claramente la pista y el HUD sin saturacion visual
- sentir que el resultado depende de habilidad y no de azar tecnico

## 5. Pilares de Diseno

### 5.1 Pilares Principales

1. Interaccion compartida clara
   Ambos jugadores deben sentir que estan viendo y jugando sobre la misma pista real.

2. Setup rapido
   La experiencia debe pasar de abrir la app a correr en menos de 2 minutos.

3. Control simple, dominio dificil
   Un solo boton debe ser suficiente para aprender, pero el timing debe crear diferencia de habilidad.

4. Legibilidad sobre espectacularidad
   La pista, los carros y el HUD deben priorizar claridad por encima de efectos pesados.

5. Alcance disciplinado
   Todo lo que no refuerce la pista compartida y la carrera funcional queda fuera del MVP.

## 6. Core Gameplay

### 6.1 Fantasia del Jugador

Estoy viendo un circuito miniatura aparecer sobre una mesa real y compito al lado de otra persona por quien controla mejor la velocidad en las curvas.

### 6.2 Core Gameplay Loop

1. Crear o unirse a una partida local.
2. Apuntar la camara al marcador fisico sobre la mesa.
3. Confirmar que la pista se ve estable y alineada.
4. Esperar a que ambos jugadores esten listos.
5. Iniciar cuenta regresiva.
6. Mantener o soltar aceleracion para completar vueltas sin penalizacion.
7. Ver resultado y elegir revancha o salir.

### 6.3 Session Loop

- abrir app
- conectar jugadores
- alinear pista
- correr una carrera corta
- revisar resultado
- repetir si hay revancha

### 6.4 Condicion de Victoria

Gana el jugador que complete primero el numero definido de vueltas.

### 6.5 Condicion de Derrota

Pierde el jugador que termine despues del oponente o acumule suficiente castigo de velocidad como para quedar atras de forma definitiva en la carrera.

### 6.6 Duracion Objetivo de Partida

- setup completo: menos de 120 segundos
- ingreso a sala local: menos de 30 segundos
- carrera: 60 a 90 segundos
- resultado y revancha: menos de 20 segundos

## 7. Mecanicas del Juego

### 7.1 Estructura de la Pista

- una sola pista cerrada y predefinida
- dos carriles fijos y paralelos, uno por jugador
- salida y meta visibles y legibles
- mezcla basica de rectas y curvas para demostrar timing

La pista no se genera proceduralmente y no cambia durante la sesion.

### 7.2 Modelo del Carro

- cada jugador controla un solo carro
- color asignado automaticamente por orden de ingreso
- jugador 1: rojo
- jugador 2: verde
- colores adicionales solo si en el futuro se habilitan mas jugadores

### 7.3 Control Principal

- un solo boton de aceleracion
- mantener presionado aumenta velocidad dentro del limite permitido
- soltar permite recuperar control antes o durante curvas

No hay freno, turbo, drift ni cambios de carril en el MVP.

### 7.4 Regla de Timing

La habilidad principal consiste en no sostener aceleracion excesiva antes y durante curvas. Si el jugador supera el umbral seguro de velocidad en una curva, recibe una penalizacion.

### 7.5 Penalizacion por Curva

Para el MVP, la penalizacion sera simple y legible:

- perdida temporal de velocidad
- breve animacion de descontrol o salida parcial del slot
- regreso automatico al carril despues de la penalizacion

Esto evita fisicas complejas y mantiene el castigo claro para el jugador.

### 7.6 Vueltas

- numero base recomendado: 3 vueltas por carrera
- contador visible en HUD
- progreso calculado sobre spline o riel fijo

### 7.7 Multijugador Local

- 2 jugadores como objetivo obligatorio
- conexion mediante hotspot o Wi-Fi comun
- un dispositivo actua como host autoritativo
- el otro entra como cliente
- 3 jugadores o mas quedan como stretch goal y no comprometen entrega

## 8. Progresion y Estructura de Sesion

### 8.1 Progresion del MVP

No existe progresion persistente entre sesiones. La progresion es de maestria:

- comprender el setup
- entender la mecanica de aceleracion
- mejorar el control en curvas
- reducir errores y tiempos por vuelta

### 8.2 Repeticion y Revancha

La revancha es el principal motor de repeticion. Si el tracking sigue estable, la experiencia debe permitir reiniciar sin rehacer todo el setup.

### 8.3 Evolucion Post-MVP

Solo si el MVP esta estable, se evaluan:

- mejor de 3 con marcador de victorias
- tercera persona en partida
- segunda pista predefinida

## 9. UX, Flujo de Interaccion y Storyboard

### 9.1 Flujo General del Usuario

1. Pantalla inicial con dos opciones: Crear partida o Unirse a partida.
2. Instrucciones cortas para mesa, marcador e iluminacion.
3. Pantalla de escaneo y anclaje de pista.
4. Confirmacion de tracking estable.
5. Espera de conexion y estado del otro jugador.
6. Cuenta regresiva e inicio.
7. Carrera con HUD minimo.
8. Pantalla de resultado.
9. Revancha o salida.

### 9.2 Principios de UX

- una sola accion principal por pantalla
- lenguaje no tecnico para el jugador
- feedback visible de tracking, conexion y estado de carrera
- HUD minimo para no tapar la pista AR
- tutorial integrado en contexto, no pantalla larga previa

### 9.3 Micro Onboarding

El onboarding debe explicar solo lo imprescindible:

- ambos jugadores deben estar en la misma red local
- ambos deben apuntar al mismo marcador sobre la mesa
- mantener presionado para acelerar
- acelerar demasiado en curva genera penalizacion

### 9.4 HUD de Carrera

Debe mostrar solo:

- boton de aceleracion grande y claro
- vuelta actual
- posicion
- mensajes breves de penalizacion o tracking perdido si aplica

### 9.5 Estados de Error y Recuperacion

El juego debe contemplar y comunicar:

- marcador no detectado
- tracking inestable
- perdida de tracking
- cliente no conectado
- red local no encontrada
- desconexion durante la carrera

Mensajes ejemplo:

- "Apunta al marcador y mueve el telefono lentamente"
- "Se perdio la alineacion. Reapunta al marcador para continuar"
- "Verifica que ambos dispositivos esten en la misma red"

### 9.6 Storyboard Textual Minimo

Escena 1: dos jugadores preparan una mesa despejada y colocan el marcador.

Escena 2: el host crea la partida y el cliente se une desde la misma red.

Escena 3: ambos apuntan la camara y la pista aparece sobre la mesa.

Escena 4: el sistema confirma que ambos estan listos y asigna colores a los carros.

Escena 5: comienza la cuenta regresiva y ambos aceleran en la salida.

Escena 6: uno de los jugadores falla una curva por exceso de velocidad y pierde tiempo.

Escena 7: se muestra el ganador y aparece la opcion de revancha.

## 10. Direccion Visual y Sonora

### 10.1 Direccion Visual

La estetica debe apoyar legibilidad y percepcion de miniatura interactiva:

- pista tipo slot car estilizada y facil de leer
- colores contrastantes entre pista, entorno y carros
- marcador integrado visualmente como base del circuito o pit board
- HUD discreto y alto contraste
- entorno 3D liviano, sin ruido visual innecesario

### 10.2 Informacion Visual

La informacion importante debe poder entenderse en menos de un segundo:

- quien soy
- en que vuelta voy
- si estoy adelante o atras
- si el tracking sigue estable

### 10.3 Audio

Audio funcional y corto:

- cuenta regresiva
- salida
- penalizacion de curva
- meta
- victoria o derrota

La musica no es prioridad del MVP. El foco esta en feedback diegetico y de interfaz.

## 11. Especificaciones Tecnicas

### 11.1 Plataforma Objetivo

- Android only para MVP
- dispositivo principal de prueba: Samsung S24 FE

### 11.2 Motor y Stack Recomendado

- Unity 2022 LTS o Unity 6 LTS, segun experiencia estable del equipo
- AR Foundation
- ARCore XR Plugin
- Netcode for GameObjects
- Unity Transport

### 11.3 Modelo de Tracking

- image tracking local
- un solo marcador fisico grande de alto contraste
- tamano recomendado: A4 grande como minimo, idealmente A3 si el espacio lo permite
- pista colocada con offset fijo respecto al marcador

### 11.4 Modelo de Networking

- topologia host-client local
- host autoritativo sobre logica de carrera
- cliente envia input y recibe estado de carrera
- no se sincroniza mundo AR crudo por red
- la red sincroniza la carrera, el marcador sincroniza la referencia espacial

### 11.5 Modelo de Simulacion

- carros sobre spline o riel fijo
- movimiento restringido por carril
- no fisica libre compleja
- variables principales: progreso, velocidad, vuelta, estado de penalizacion

### 11.6 Requerimientos de Rendimiento

- 30 FPS como minimo estable en dispositivo objetivo
- sin crashes durante una sesion completa de 5 minutos
- calentamiento tolerable sin degradacion grave de experiencia

### 11.7 Software y Hardware a Utilizar

Software:

- Unity
- Android build support
- AR Foundation y ARCore
- sistema de control UI simple

Hardware:

- minimo 2 telefonos Android compatibles con ARCore
- una mesa despejada
- un marcador fisico impreso
- red local por hotspot o Wi-Fi compartido

## 12. Requerimientos de Produccion

### 12.1 Elementos Digitales

- pista 3D optimizada
- 2 modelos de carro o 1 modelo con 2 materiales
- HUD y pantallas basicas
- efectos visuales ligeros para penalizacion, salida y meta
- audio funcional corto

### 12.2 Elementos Fisicos

- marcador impreso y estable
- superficie de mesa apta para visibilidad y tracking
- guia rapida de montaje para pruebas y demo

### 12.3 Documentacion Necesaria

- GDD
- storyboard
- cronograma
- lista de pruebas
- guia de demo y montaje

## 13. Alcance Final del MVP

### 13.1 In Scope

- juego funcional Android
- 2 jugadores locales
- 1 marcador compartido
- 1 pista predefinida
- 1 modo carrera rapida
- 1 boton de aceleracion
- 3 vueltas por carrera
- resultado y revancha

### 13.2 Out of Scope

- iOS
- juego online
- soporte garantizado para 3 o mas jugadores
- personalizacion o seleccion de carro
- multiples pistas en primera entrega
- procedural generation
- power-ups, freno, turbo, drift
- cambio de carril
- progression persistente
- economia o desbloqueos

### 13.3 Stretch Goals

- tercer jugador
- segunda pista
- marcador de mejor de 3
- mejoras de audio y polish visual

## 14. Epics de Desarrollo

### Epic 1 - Base AR y Tracking

Objetivo: detectar marcador, anclar pista y validar estabilidad en dispositivo real.

### Epic 2 - Gameplay Slot Car

Objetivo: implementar pista funcional, carros sobre carril, aceleracion, vueltas y penalizacion.

### Epic 3 - Multijugador Local

Objetivo: crear flujo host-client, lobby basico y sincronizacion de carrera 2P.

### Epic 4 - UX y Presentacion de Sesion

Objetivo: onboarding, HUD, estados de error, resultado y revancha.

### Epic 5 - QA, Optimizacion y Demo Final

Objetivo: estabilizar tracking, red, rendimiento, demo repetible y documentacion final.

## 15. Roles y Responsabilidades Sugeridas

Si el equipo es pequeno, una persona puede cubrir mas de un frente. Las responsabilidades minimas son:

- Integracion AR y tracking
- Networking y flujo de partida
- Gameplay y logica de carrera
- UX/UI/HUD y soporte visual
- QA, pruebas en dispositivo y documentacion

## 16. Cronograma Propuesto

### Semana 1 - 21 abr al 27 abr

- configurar proyecto Unity Android
- integrar AR Foundation y ARCore
- detectar marcador en Samsung S24 FE
- instanciar pista dummy sobre marcador
- validar estabilidad de tracking

### Semana 2 - 28 abr al 4 may

- implementar pista funcional y spline
- crear carro base y control de aceleracion
- implementar vueltas y penalizacion basica
- construir menu minimo y flujo de escaneo

### Semana 3 - 5 may al 11 may

- integrar multijugador local 2P
- crear host y cliente
- sincronizar countdown, estado de carrera y resultado
- hacer pruebas con dos dispositivos Android

### Semana 4 - 12 may al 18 may

- mejorar HUD, feedback y onboarding
- corregir tracking, orientacion y escala
- balancear velocidad y castigo en curvas
- hacer pruebas con usuarios no tecnicos

### Semana 5 - 19 may al 26 may

- corregir bugs criticos
- optimizar rendimiento
- cerrar demo, guia de uso y build final
- preparar presentacion y ensayo de montaje

## 17. Riesgos y Mitigaciones

### Riesgo 1 - Tracking inestable

Mitigacion:

- usar un marcador grande y de alto contraste
- probar en condiciones reales desde la primera semana
- no iniciar partida hasta confirmar estabilidad

### Riesgo 2 - Desalineacion entre dispositivos

Mitigacion:

- usar la misma referencia fisica para ambos
- anclar pista con offset fijo
- evitar sincronizacion del mundo AR por red

### Riesgo 3 - Fallas de red local

Mitigacion:

- usar host local simple
- probar con hotspot real desde semana 3
- mostrar mensajes de error claros y reinicio rapido

### Riesgo 4 - Scope creep

Mitigacion:

- congelar 2 jugadores como contrato de entrega
- bloquear procedural, iOS y extras competitivos
- mover todo lo no esencial a stretch goals

### Riesgo 5 - Rendimiento y calor del dispositivo

Mitigacion:

- simplificar arte y efectos
- medir rendimiento en dispositivo real
- mantener escena pequena y estable

## 18. Plan de QA y Criterios de Aceptacion

### 18.1 Criterios de Aceptacion del MVP

- dos jugadores pueden iniciar una carrera completa sobre la misma mesa
- ambos ven la misma pista alineada respecto al marcador
- el juego pasa de apertura a carrera en menos de 2 minutos
- el cliente puede unirse a la sesion en menos de 30 segundos
- la pista se mantiene estable durante una carrera completa
- el juego corre a 30 FPS o mas en el dispositivo objetivo
- un usuario nuevo entiende el control despues de una partida

### 18.2 Pruebas Minimas

- prueba de tracking con diferentes condiciones de luz
- prueba de red local con hotspot
- prueba de sesion completa de inicio a fin
- prueba de revancha sin rehacer setup completo
- prueba prolongada de 5 minutos para calor y estabilidad
- prueba con usuario no tecnico para validar learnability

## 19. Metricas de Exito

### 19.1 Metricas Tecnicas

- FPS estable >= 30
- tasa de carrera completada >= 90 por ciento en pruebas internas
- setup total <= 120 segundos
- join local <= 30 segundos

### 19.2 Metricas de Experiencia

- usuarios entienden como empezar sin explicacion larga
- usuarios identifican claramente cuando acelerar y cuando no
- usuarios reportan que ambos estan viendo la misma experiencia
- al menos una revancha espontanea en pruebas de usuario indica deseo de repeticion

## 20. Supuestos y Dependencias

### 20.1 Supuestos

- el equipo continuara desarrollando en Unity
- se dispone de al menos dos telefonos Android compatibles con ARCore
- la demo ocurrira en un entorno interior controlable
- la materia evaluara experiencia funcional y claridad de interaccion

### 20.2 Dependencias

- disponibilidad de un segundo dispositivo Android para pruebas 2P
- eleccion final de version de Unity por parte del equipo
- diseno final del marcador fisico

## 21. Decision de Diseno Mas Importante

El valor del proyecto no esta en la profundidad del sistema de carreras, sino en la calidad de la experiencia compartida: ver la misma pista AR sobre una mesa real y competir alrededor de ella con una interaccion simple, clara y repetible.

Ese principio debe gobernar todas las decisiones de produccion, diseno y alcance.