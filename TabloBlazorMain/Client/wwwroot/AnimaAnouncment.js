export function animationAnnouncment() {
    var elem = document.querySelector('#annoncment');
    var flkty = new Flickity(elem, {
        autoPlay: 10000,
        pauseAutoPlayOnHover: false,
        prevNextButtons: false,
        pageDots: false,
        wrapAround: true
    });

}
export function vanila() {
    var elem = document.querySelector('#annoncment');
    var flkty = new Flickity(elem, {
        autoPlay: 10000,
        pauseAutoPlayOnHover: false,
        prevNextButtons: false,
        pageDots: false,
        wrapAround: true
    });

}

export function create() {
    var flick = Flickity.data('#annoncment');
    flick.destroy();
}

export function refresh() {
    setTimeout(function () {
        location.reload();
    }, 10000);
}

export function blink() {
    var text = document.querySelectorAll(".dec");
    for (let item of text) {
        let dateBegin = new Date();
        let dateEnd = new Date();
        dateBegin.setHours(+item.innerText.substring(8, 10));
        dateBegin.setMinutes(+item.innerText.substring(11, 13));

        dateEnd.setHours(item.innerText.substring(item.innerText.length - 5, item.innerText.length - 3));
        dateEnd.setMinutes(item.innerText.substring(item.innerText.length - 2, item.innerText.length));

        if (item.innerText.includes("Начало") && dateBegin > new Date()) {
            if (!item.classList.contains("blink1")) {
                item.classList.add("blink1");
            }
        }
        else if (item.innerHTML.includes("Конец") && dateEnd < new Date()) {
            if (!item.classList.contains("blink1")) {
                item.classList.add("blink1");
            }
        }
        else {
            if (item.classList.contains("blink1")) {
                item.classList.remove("blink1");
            }
        }
    }
}

export function poppersLoad() {
    const body = document.querySelector('.mainBody');
    body.onclick = function (event) {
        let removeList = document.querySelectorAll(".popover");
        removeList.forEach((item) => {
            if (item.id != event.target.getAttribute("aria-describedby"))
                item.remove()
        });
    }
   
}

export function clickTeacher(teach) {
    let list = document.querySelectorAll('[data-toggle="popover"]')
    list.forEach((item) => {
        if (teach == item.innerText) {
            let sks = new bootstrap.Popover(item, { trigger: "manual", html: true })
            
            sks.show();
        }
    });
}
