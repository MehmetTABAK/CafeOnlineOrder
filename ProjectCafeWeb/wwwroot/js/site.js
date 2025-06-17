document.addEventListener('DOMContentLoaded', () => {
    const initLayout = () => {
        const toggleSidebar = () => {
            const sidebar = document.querySelector('.sidebar');
            const content = document.querySelector('.content');

            sidebar.classList.toggle('hidden');
            content.classList.toggle('collapsed');
        };

        const toggleSubmenu = (event) => {
            event.stopPropagation();
            const parent = event.target.closest('li');
            const submenu = parent.querySelector('ul');
            parent.classList.toggle('open');
            submenu.style.display = parent.classList.contains('open') ? 'block' : 'none';
            const arrow = parent.querySelector('.toggle-submenu');
            arrow.style.transform = parent.classList.contains('open') ? 'rotate(180deg)' : 'rotate(0deg)';
        };

        const toggleUserMenu = (event) => {
            const userSection = event.target.closest('.user-section');
            userSection.classList.toggle('open');
        };

        document.querySelector('.toggle-btn').addEventListener('click', toggleSidebar);
        document.querySelectorAll('.sidebar > ul > li').forEach(item => {
            item.addEventListener('click', toggleSubmenu);
        });
        document.querySelectorAll('.user-section').forEach(item => {
            item.addEventListener('click', toggleUserMenu);
        });
    };

    initLayout();
});

document.addEventListener('DOMContentLoaded', () => {
    const initBlogPage = () => {
        const imageUpload = document.getElementById('image-upload');
        const preview = document.getElementById('preview');
        const placeholder = document.querySelector('.upload-area #Capa_1');
        const imgText = document.querySelector('.upload-area .imgtext');
        const fileInfo = document.getElementById('file-info');
        const removeImageBtn = document.getElementById('remove-image');
        const uploadArea = document.querySelector('.upload-area');
        const imageBtn = document.getElementById('image-btn');
        const imageInput = document.getElementById('image-input');
        const discountRadios = document.querySelectorAll('input[name="discount"]');
        const discountInput = document.getElementById("discount-amount");
        const discountLabel = document.getElementById("discount-label");

        // **Resim yükleme işlemi:**
        if (imageUpload) {
            imageUpload.addEventListener('change', function () {
                const file = this.files[0];
                if (file) {
                    const reader = new FileReader();
                    reader.onload = function (e) {
                        preview.src = e.target.result;
                        preview.style.display = 'block';
                        placeholder.style.display = 'none';
                        imgText.style.display = 'none';
                        fileInfo.textContent = `Seçilen dosya: ${file.name}`;
                        preview.onload = function () {
                            uploadArea.style.height = `${preview.height}px`;
                            uploadArea.style.width = `${preview.width}px`;
                        };
                    };
                    reader.readAsDataURL(file);
                } else {
                    resetImageUpload();
                }
            });

            function resetImageUpload() {
                imageUpload.value = '';
                preview.src = '';
                preview.style.display = 'none';
                placeholder.style.display = 'block';
                imgText.style.display = 'block';
                uploadArea.style.height = '200px';
                uploadArea.style.width = '250px';
                fileInfo.textContent = 'Kapak resmi seçilmedi.';
            }
        }

        discountRadios.forEach(radio => {
            radio.addEventListener("change", function () {
                if (this.value === "yes") {
                    discountContainer.classList.remove('discount-hidden');
                } else {
                    discountContainer.classList.add('discount-hidden');
                    discountInput.value = ""; // Temizle
                }
            });
        });
    };

    initBlogPage();
});

document.addEventListener('DOMContentLoaded', () => {
    const publicKeyUrl = '/Notification/PublicKey';

    async function registerServiceWorkerAndSubscribe() {
        if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
            console.warn("Push mesajları desteklenmiyor.");
            return;
        }

        const registration = await navigator.serviceWorker.register('/js/service-worker.js');

        const publicKeyResponse = await fetch(publicKeyUrl);
        const publicKey = await publicKeyResponse.text();

        const subscription = await registration.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: urlBase64ToUint8Array(publicKey)
        });

        const p256dhKey = subscription.getKey('p256dh');
        const authKey = subscription.getKey('auth');

        const p256dh = p256dhKey ? btoa(String.fromCharCode(...new Uint8Array(p256dhKey))) : null;
        const auth = authKey ? btoa(String.fromCharCode(...new Uint8Array(authKey))) : null;

        // Aboneliği sunucuya gönder
        await fetch('/Notification/Subscribe', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                Endpoint: subscription.endpoint,
                P256dh: p256dh,
                Auth: auth
            })
        });
    }

    // Base64 public key decode fonksiyonu
    function urlBase64ToUint8Array(base64String) {
        const padding = '='.repeat((4 - base64String.length % 4) % 4);
        const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
        const rawData = atob(base64);
        return Uint8Array.from([...rawData].map(char => char.charCodeAt(0)));
    }

    // Sayfa yüklendiğinde başlat
    window.addEventListener('load', () => {
        registerServiceWorkerAndSubscribe();
    });
});