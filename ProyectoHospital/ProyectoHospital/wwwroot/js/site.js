// Selector de colores para presentar las opciones de HU03 al cliente.
document.querySelectorAll("[data-tema-opcion]").forEach(boton => {
    boton.addEventListener("click", () => {
        document.body.dataset.tema = boton.dataset.temaOpcion;
        document.querySelectorAll("[data-tema-opcion]").forEach(opcion =>
            opcion.setAttribute("aria-pressed", String(opcion === boton)));
    });
});
