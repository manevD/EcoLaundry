(function () {

    function cyrToLat(text) {

        if (!text)
            return "";

        text = text.toLowerCase();

        const map = {
            "а": "a", "б": "b", "в": "v", "г": "g", "д": "d",
            "ѓ": "gj", "е": "e", "ж": "zh", "з": "z", "ѕ": "dz",
            "и": "i", "ј": "j", "к": "k", "л": "l", "љ": "lj",
            "м": "m", "н": "n", "њ": "nj", "о": "o", "п": "p",
            "р": "r", "с": "s", "т": "t", "ќ": "kj", "у": "u",
            "ф": "f", "х": "h", "ц": "c", "ч": "ch", "џ": "dj",
            "ш": "sh"
        };

        let result = "";

        for (let i = 0; i < text.length; i++) {

            result += map[text[i]] ?? text[i];

        }

        return result;
    }

    function latToCyr(text) {

        if (!text)
            return "";

        text = text.toLowerCase();

        return text

            .replace(/sh/g, "ш")
            .replace(/zh/g, "ж")
            .replace(/ch/g, "ч")
            .replace(/lj/g, "љ")
            .replace(/nj/g, "њ")
            .replace(/gj/g, "ѓ")
            .replace(/kj/g, "ќ")
            .replace(/dz/g, "ѕ")
            .replace(/dj/g, "џ")

            .replace(/a/g, "а")
            .replace(/b/g, "б")
            .replace(/v/g, "в")
            .replace(/g/g, "г")
            .replace(/d/g, "д")
            .replace(/e/g, "е")
            .replace(/z/g, "з")
            .replace(/i/g, "и")
            .replace(/j/g, "ј")
            .replace(/k/g, "к")
            .replace(/l/g, "л")
            .replace(/m/g, "м")
            .replace(/n/g, "н")
            .replace(/o/g, "о")
            .replace(/p/g, "п")
            .replace(/r/g, "р")
            .replace(/s/g, "с")
            .replace(/t/g, "т")
            .replace(/u/g, "у")
            .replace(/f/g, "ф")
            .replace(/h/g, "х")
            .replace(/c/g, "ц");
    }

    window.attachSearch = function (inputId, selector) {

        const input = document.getElementById(inputId);

        if (!input)
            return;

        input.addEventListener("input", function () {

            const search = this.value.trim().toLowerCase();

            const latin = cyrToLat(search);

            const cyr = latToCyr(search);

            document.querySelectorAll(selector).forEach(item => {

                const text = item.innerText.toLowerCase();

                const latinText = cyrToLat(text);

                item.style.display =
                    text.includes(search) ||
                        latinText.includes(latin) ||
                        text.includes(cyr)
                        ? ""
                        : "none";

            });

        });

    };

})();