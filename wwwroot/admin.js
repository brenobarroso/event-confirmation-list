const eventFormEl = document.getElementById("event-form");
const eventFeedbackEl = document.getElementById("event-feedback");
const guestListEl = document.getElementById("guest-list");
const totalGuestsEl = document.getElementById("total-guests");

async function ensureAdminSession() {
  const response = await fetch("/api/admin/session");
  if (!response.ok) {
    window.location.href = "/";
    return false;
  }

  return true;
}

async function loadAdminData() {
  try {
    const response = await fetch("/api/admin/dados");
    if (!response.ok) {
      throw new Error("Falha ao carregar dados do admin");
    }

    const data = await response.json();
    const eventInfo = data.eventInfo || {};

    document.getElementById("title").value = eventInfo.title || "";
    document.getElementById("date").value = eventInfo.date || "";
    document.getElementById("time").value = eventInfo.time || "";
    document.getElementById("location").value = eventInfo.location || "";

    totalGuestsEl.textContent = data.totalGuests || 0;
    guestListEl.innerHTML = "";

    const guests = data.confirmedGuests || [];
    if (guests.length === 0) {
      const emptyItem = document.createElement("li");
      emptyItem.textContent = "Nenhuma confirmação ainda.";
      guestListEl.appendChild(emptyItem);
      return;
    }

    guests.forEach((name) => {
      const item = document.createElement("li");
      item.textContent = name;
      guestListEl.appendChild(item);
    });
  } catch (error) {
    eventFeedbackEl.textContent = "Erro ao carregar dados do painel.";
    eventFeedbackEl.classList.add("error");
  }
}

eventFormEl.addEventListener("submit", async (event) => {
  event.preventDefault();
  eventFeedbackEl.classList.remove("error");

  const payload = {
    title: document.getElementById("title").value.trim(),
    date: document.getElementById("date").value.trim(),
    time: document.getElementById("time").value.trim(),
    location: document.getElementById("location").value.trim()
  };

  try {
    const response = await fetch("/api/evento", {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify(payload)
    });

    if (!response.ok) {
      throw new Error("Falha ao salvar evento");
    }

    eventFeedbackEl.textContent = "Evento salvo com sucesso.";
    await loadAdminData();
  } catch (error) {
    eventFeedbackEl.textContent = "Não foi possível salvar o evento.";
    eventFeedbackEl.classList.add("error");
  }
});

async function initAdminPage() {
  const hasSession = await ensureAdminSession();
  if (!hasSession) {
    return;
  }

  await loadAdminData();
}

initAdminPage();
