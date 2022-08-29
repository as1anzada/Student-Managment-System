const elm_log = document.querySelector("#log")
        const elm_roots = document.querySelector("#roots")

        const test = (a, b) => {
            const res = linearSolve(a, b)
            console.log(res)

            const display = (value) => {
                if(value.i === 0)
                    return `${value.real}`
                else
                    return `${value.real} + ${value.i} <i>i</i>`
            }
            elm_roots.innerHTML = `X<sub>1</sub> = ${display(res[0])}<br>X<sub>2</sub> = ${display(res[1])}<br>`
        }

        window.addEventListener("load", () => {
            document.querySelector("button").click()
        })
    