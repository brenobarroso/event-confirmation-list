const titleEl = document.getElementById("event-title");
const dateEl = document.getElementById("event-date");
const timeEl = document.getElementById("event-time");
const locationEl = document.getElementById("event-location");
const formEl = document.getElementById("rsvp-form");
const nameInputEl = document.getElementById("guest-name");
const feedbackEl = document.getElementById("rsvp-feedback");

async function loadEventInfo() {
  try {
    const response = await fetch("/api/evento");

    if (!response.ok) {
      titleEl.textContent = "Evento ainda não cadastrado";
      return;
    }

    const eventData = await response.json();
    titleEl.textContent = eventData.title || "Evento sem título";
    dateEl.textContent = eventData.date || "-";
    timeEl.textContent = eventData.time || "-";
    locationEl.textContent = eventData.location || "-";
  } catch (error) {
    titleEl.textContent = "Erro ao carregar evento";
  }
}

formEl.addEventListener("submit", async (event) => {
  event.preventDefault();
  feedbackEl.classList.remove("error");

  const name = nameInputEl.value.trim();
  if (!name) {
    feedbackEl.textContent = "Informe seu nome para confirmar.";
    feedbackEl.classList.add("error");
    return;
  }

  try {
    const response = await fetch("/api/confirmar", {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify({ name })
    });

    if (!response.ok) {
      throw new Error("Falha ao confirmar presença");
    }

    feedbackEl.textContent = "Presença confirmada. Obrigado!";
    nameInputEl.value = "";
  } catch (error) {
    feedbackEl.textContent = "Não foi possível confirmar agora. Tente novamente.";
    feedbackEl.classList.add("error");
  }
});

loadEventInfo();
